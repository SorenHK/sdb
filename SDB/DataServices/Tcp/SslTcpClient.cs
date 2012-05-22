using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace SDB.DataServices.Tcp
{
    public class SslTcpClient : TcpClient
    {
        private readonly string _serverName;

        public SslTcpClient(string serverName, IPAddress ip, int dataPort = TcpServer.DefaultDataPort, int eventPort = TcpServer.DefaultEventPort)
            : base(ip, dataPort, eventPort)
        {
            _serverName = serverName;
        }

        public SslTcpClient(string serverName, string host, int dataPort = TcpServer.DefaultDataPort, int eventPort = TcpServer.DefaultEventPort)
            : base(host, dataPort, eventPort)
        {
            _serverName = serverName;
        }

        protected override Stream OnPrepareStream(NetworkStream stream)
        {
            var sslStream = new SslStream(stream, false, ValidateServerCertificate, null);

            try
            {
                // The server name must match the name on the server certificate.
                sslStream.AuthenticateAsClient(_serverName);
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
                return null;
            }

            return sslStream;
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }
    }
}
