using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SDB.DataServices.Tcp
{
    public class SslTcpServer : TcpServer
    {
        private readonly X509Certificate _certificate;

        public SslTcpServer(X509Certificate certificate, int dataPort = DefaultDataPort, int eventPort = DefaultEventPort) 
            : base(dataPort, eventPort)
        {
            _certificate = certificate;
        }

        protected override Stream OnPrepareStream(NetworkStream stream)
        {
            var sslStream = new SslStream(stream, false);

            try
            {
                sslStream.AuthenticateAsServer(_certificate, false, SslProtocols.Tls, true);
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
                sslStream.Close();
                return null;
            }

            return sslStream;
        }
    }
}
