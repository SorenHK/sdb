using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using SDB.Helpers;

namespace SDB.DataServices.Tcp
{
    public class EncryptedTcpClient : TcpClient
    {
        private RSACryptographyHandler _asyncServerCryptographyHandler;
        private RSACryptographyHandler _asyncClientCryptographyHandler;
        private AESCryptographyHandler _syncCryptographyHandler;

        public string ServerPublicKey { get { return _asyncServerCryptographyHandler.PublicKey; } }

        public EncryptedTcpClient(IPAddress ip, int dataPort = TcpServer.DefaultDataPort, int eventPort = TcpServer.DefaultEventPort, string serverPublicKey = null)
            : base(ip, dataPort, eventPort)
        {
            Init(serverPublicKey);
        }

        public EncryptedTcpClient(string host, int dataPort = TcpServer.DefaultDataPort, int eventPort = TcpServer.DefaultEventPort, string serverPublicKey = null)
            : base(host, dataPort, eventPort)
        {
            Init(serverPublicKey);
        }

        private void Init(string serverPublicKey = null)
        {
            _asyncServerCryptographyHandler = new RSACryptographyHandler();
            _asyncClientCryptographyHandler = new RSACryptographyHandler { IsReady = true };
            _syncCryptographyHandler = new AESCryptographyHandler();

            // Send client's RSA public key and request the server's RSA public key
            var request = new TcpMessage("rsakey") { Content = _asyncClientCryptographyHandler.PublicKey };
            var response = SendAndReceive(request);
            if (response.HasType("rsakey"))
            {
                var key = response.Content;
                if (!string.IsNullOrEmpty(serverPublicKey) && !serverPublicKey.Equals(key))
                    throw new Exception("Server did not return correct public key");
                _asyncServerCryptographyHandler.PublicKey = key;
            }

            // Request the AES key from the server
            request = new TcpMessage("aeskey");
            response = SendAndReceive(request);
            if (response.HasType("aeskey"))
                _syncCryptographyHandler.KeyAsString = response.Content;

            // Request the AES initialization vector from the server
            request = new TcpMessage("aesiv");
            response = SendAndReceive(request);
            if (response.HasType("aesiv"))
                _syncCryptographyHandler.InitializationVectorAsString = response.Content;
        }

        protected override string PrepareIncommingMessage(string message)
        {
            if (_syncCryptographyHandler != null && _syncCryptographyHandler.IsReady)
                message = _syncCryptographyHandler.Decrypt(message);
            else if (_asyncServerCryptographyHandler.IsReady)
                message = _asyncClientCryptographyHandler.Decrypt(message);

            return base.PrepareIncommingMessage(message);
        }

        protected override string PrepareOutgoingMessage(string message)
        {
            if (_syncCryptographyHandler != null && _syncCryptographyHandler.IsReady)
                message = _syncCryptographyHandler.Encrypt(message);
            else if (_asyncServerCryptographyHandler.IsReady)
                message = _asyncServerCryptographyHandler.Encrypt(message);

            return base.PrepareOutgoingMessage(message);
        }
    }
}
