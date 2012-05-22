using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SDB.DataServices.Tcp
{
    public delegate TcpMessage TcpRequestHandler(TcpConnectedHost host, TcpMessage message);

    public class TcpServer : IDisposable
    {
        public const int DefaultDataPort = 7320;
        public const int DefaultEventPort = 7321;

        private readonly TcpListener _dataTcpListener;
        private readonly Thread _dataListenThread;
        private readonly TcpListener _eventTcpListener;
        private readonly Thread _eventListenThread;
        private readonly LinkedList<TcpMessageQueue> _eventQueues;
        private readonly LinkedList<TcpConnectedHost> _connectedDataClients;
        private readonly LinkedList<TcpRequestHandler> _dataRequestHandlers;
        private readonly LinkedList<ITcpAuthenticationProvider> _authenticationProviders;
        private readonly LinkedList<string> _whitelistedAddresses;

        private bool _keepRunning;

        public bool AllowAll { get; set; }

        public TcpServer(int dataPort = DefaultDataPort, int eventPort = DefaultEventPort)
        {
            AllowAll = true;
            _whitelistedAddresses = new LinkedList<string>();

            _keepRunning = true;
            _eventQueues = new LinkedList<TcpMessageQueue>();
            _dataRequestHandlers = new LinkedList<TcpRequestHandler>();
            _connectedDataClients = new LinkedList<TcpConnectedHost>();
            _authenticationProviders = new LinkedList<ITcpAuthenticationProvider>();

            _dataTcpListener = new TcpListener(IPAddress.Any, dataPort);
            _eventTcpListener = new TcpListener(IPAddress.Any, eventPort);

            _dataListenThread = new Thread(ListenForDataClients);
            _eventListenThread = new Thread(ListenForEventClients);

            _dataListenThread.Start();
            _eventListenThread.Start();
        }

        private void ListenForDataClients()
        {
            ListenForClients(_dataTcpListener, HandleDataClientComm);
        }

        private void ListenForEventClients()
        {
            ListenForClients(_eventTcpListener, HandleEventClientComm);
        }

        private void ListenForClients(TcpListener listener, Action<object> clientHandler)
        {
            listener.Start();

            while (_keepRunning)
            {
                try
                {
                    //blocks until a client has connected to the server
                    var client = listener.AcceptTcpClient();

                    var clientThread = new Thread(new ParameterizedThreadStart(clientHandler));
                    clientThread.Start(client);
                }
                catch (SocketException e)
                {
                    Debug.WriteLine("Exception in ListenForClients: " + e);
                    break; // Stop listening for clients (socket exceptions may happen when the TcpListener is stopped/disposed)
                }
            }
        }

        protected virtual Stream OnPrepareStream(NetworkStream stream)
        {
            return stream;
        }

        private void HandleDataClientComm(object client)
        {
            var tcpClient = (System.Net.Sockets.TcpClient)client;

            var addressSplits = tcpClient.Client.RemoteEndPoint.ToString().Split(':');

            var host = new TcpConnectedHost
            {
                TcpClient = tcpClient,
                IPAddress = addressSplits[0]
            };

            if (!IsAllowed(host.IPAddress))
            {
                Debug.WriteLine("Client refused access to data channel. IP: " + host.IPAddress);
                tcpClient.Close();
                return;
            }

            Debug.WriteLine("Client connected to data channel. IP: " + host.IPAddress);

            var stream = OnPrepareStream(host.TcpClient.GetStream());

            _connectedDataClients.AddLast(host);

            while (true)
            {
                var rawRequest = TcpProtocolHelper.Read(stream);

                if (rawRequest == null)
                    break;

                rawRequest = PrepareIncommingMessage(host, rawRequest);

                Debug.WriteLine(DateTime.Now.ToLongTimeString() + " [R] " + rawRequest);

                TcpMessage response = null;

                var request = TcpMessage.FromRaw(rawRequest);

                foreach (var handler in _dataRequestHandlers)
                {
                    try
                    {
                        response = handler.Invoke(host, request);
                        if (response != null)
                            break;
                    }
                    catch(Exception e)
                    {
                        response = TcpMessage.Error(e.ToString());
                        break;
                    }
                }

                if (response == null)
                    response = TcpMessage.Error("Could not handle request");

                var responseMessage = response.ToString();

                Debug.WriteLine(DateTime.Now.ToLongTimeString() + " [S] " + responseMessage);

                responseMessage = PrepareOutgoingMessage(host, responseMessage);

                var success = TcpProtocolHelper.Write(stream, responseMessage);
                if (!success)
                    break;
            }

            Debug.WriteLine("Client disconnected from data channel. IP: " + host.IPAddress);
            tcpClient.Close();
        }

        private void HandleEventClientComm(object client)
        {
            Debug.WriteLine("Client connected to event channel");

            var tcpClient = (System.Net.Sockets.TcpClient)client;
            var stream = OnPrepareStream(tcpClient.GetStream());

            var eventQueue = new TcpMessageQueue();
            lock (_eventQueues)
            {
                _eventQueues.AddLast(eventQueue);
            }

            while (true)
            {
                var messages = eventQueue.WaitAndPopAll();

                var success = true;
                foreach (var message in messages)
                {
                    success = TcpProtocolHelper.Write(stream, message);
                    if (!success)
                        break;
                }

                if (!success)
                    break;
            }

            lock (_eventQueues)
            {
                _eventQueues.Remove(eventQueue);
            }

            Debug.WriteLine("Client disconnected from event channel");
        }

        protected virtual string PrepareIncommingMessage(TcpConnectedHost host, string message)
        {
            return message;
        }

        protected virtual string PrepareOutgoingMessage(TcpConnectedHost host, string message)
        {
            return message;
        }

        public void Register(TcpRequestHandler handler)
        {
            _dataRequestHandlers.AddLast(handler);
        }

        public void Register(ITcpAuthenticationProvider authenticationProvider)
        {
            _authenticationProviders.AddLast(authenticationProvider);
        }

        public void Enqueue(TcpMessage message)
        {
            DoOnQueues(q => q.Add(message));
        }

        public void Allow(IPAddress ip)
        {
            if (ip == null)
                return;

            Allow(ip.ToString());
        }

        public void Allow(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                return;

            AllowAll = false;

            var ipComponents = ip.Split('.');
            if (ipComponents.Length > 4)
                throw new ArgumentException("Malformed IP-address: too many components.", "ip");

            _whitelistedAddresses.AddLast(ip);
        }

        private bool IsAllowed(string ip)
        {
            if (AllowAll)
                return true;

            if (string.IsNullOrEmpty(ip))
                return false;

            var ipComponents = ip.Split('.');
            if (ipComponents.Length < 4)
                return false;

            // Todo: implement and check blacklist

            foreach (var address in _whitelistedAddresses)
            {
                if (address.Equals(ip))
                    return true;

                if (address.Contains("*"))
                {
                    var splits = address.Split('.');
                    for (var i = 0; i < splits.Length; i++)
                    {
                        if (!splits[i].Equals("*") && !splits[i].Equals(ipComponents[i]))
                            break;

                        if (i == splits.Length - 1)
                            return true;
                    }
                }
            }

            return false;
        }

        private void DoOnQueues(Action<TcpMessageQueue> action)
        {
            if (action == null)
                return;

            lock (_eventQueues)
            {
                foreach (var queue in _eventQueues)
                {
                    action.Invoke(queue);
                }
            }
        }

        public void Dispose()
        {
            _keepRunning = false;
            if (_dataTcpListener != null)
                _dataTcpListener.Stop();
            if (_eventTcpListener != null)
                _eventTcpListener.Stop();
            // Todo: close all connections
        }
    }
}
