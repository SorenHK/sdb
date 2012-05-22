using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SDB.DataServices.Tcp
{
    public delegate bool TcpMessageHandler(TcpMessage message);

    public class TcpClient : IDisposable
    {
        private readonly string _host;
        private readonly int _dataPort;
        private readonly int _eventPort;
        private System.Net.Sockets.TcpClient _dataClient;
        private System.Net.Sockets.TcpClient _eventClient;
        private Stream _dataStream;
        private Stream _eventStream;
        private readonly Thread _eventThread;
        private readonly object _dataLockObject;
        private bool _listenForEvents;
        private LinkedList<TcpMessageHandler> _eventHandlers; 

        public TcpClient(IPAddress ip, int dataPort = TcpServer.DefaultDataPort, int eventPort = TcpServer.DefaultEventPort)
            : this(ip.ToString(), dataPort, eventPort)
        {
        }

        public TcpClient(string host, int dataPort = TcpServer.DefaultDataPort, int eventPort = TcpServer.DefaultEventPort)
        {
            _host = host;
            _dataPort = dataPort;
            _eventPort = eventPort;
            _dataLockObject = new object();
            _eventHandlers = new LinkedList<TcpMessageHandler>();

            _listenForEvents = true;

            _eventThread = new Thread(ListenForEvents);
            _eventThread.Start();
        }

        private void ListenForEvents()
        {
            _eventClient = new System.Net.Sockets.TcpClient();
            _eventClient.Connect(_host, _eventPort);

            _eventStream = OnPrepareStream(_eventClient.GetStream());

            while (_listenForEvents)
            {
                var rawMessage = TcpProtocolHelper.Read(_eventStream);
                if (rawMessage == null)
                    break;

                var message = TcpMessage.FromRaw(rawMessage);

                foreach(var handler in _eventHandlers)
                {
                    if (handler(message))
                        break;
                }
            }
        }

        protected virtual Stream OnPrepareStream(NetworkStream stream)
        {
            return stream;
        }

        public void RegisterEventHandler(TcpMessageHandler handler)
        {
            _eventHandlers.AddLast(handler);
        }

        public string SendAndReceive(string message)
        {
            lock (_dataLockObject)
            {
                if (_dataClient == null)
                {
                    _dataClient = new System.Net.Sockets.TcpClient();

                    _dataClient.Connect(_host, _dataPort);

                    _dataStream = OnPrepareStream(_dataClient.GetStream());
                }

                message = PrepareOutgoingMessage(message);

                var success = TcpProtocolHelper.Write(_dataStream, message);
                if (!success)
                    return null;

                return PrepareIncommingMessage(TcpProtocolHelper.Read(_dataStream));
            }
        }

        public TcpMessage SendAndReceive(TcpMessage request)
        {
            return TcpMessage.FromRaw(SendAndReceive(request.ToString()));
        }

        public ObjectTcpMessage<T> SendAndReceive<T>(TcpMessage request) where T : class
        {
            return new ObjectTcpMessage<T>(SendAndReceive(request));
        }

        protected virtual string PrepareIncommingMessage(string message)
        {
            return message;
        }

        protected virtual string PrepareOutgoingMessage(string message)
        {
            return message;
        }

        public void Dispose()
        {
            if (_dataClient != null)
                _dataClient.Close();
            _listenForEvents = false;
            if (_eventThread != null)
                _eventThread.Abort();
            if (_eventClient != null)
                _eventClient.Close();
        }
    }
}
