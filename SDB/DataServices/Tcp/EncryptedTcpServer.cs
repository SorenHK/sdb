using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SDB.Helpers;

namespace SDB.DataServices.Tcp
{
    public class EncryptedTcpServer : TcpServer
    {
        private readonly RSACryptographyHandler _serverCryptographyHandler;
        private readonly Dictionary<TcpConnectedHost, EncryptedClientDataContainer> _clients;

        public string ServerPublicPrivateKey { get { return _serverCryptographyHandler.PrivateKey; } }

        public EncryptedTcpServer(int dataPort = DefaultDataPort, int eventPort = DefaultEventPort, bool storeKeysInConfiguration = false)
            : this(ConfigurationHelper.Get("tcp", "rsapublicprivatekey"), dataPort, eventPort)
        {
            if (storeKeysInConfiguration && !_serverCryptographyHandler.IsReady)
                ConfigurationHelper.Set("tcp", "rsapublicprivatekey", _serverCryptographyHandler.PrivateKey);
        }

        public EncryptedTcpServer(string serverPublicPrivateKey, int dataPort = DefaultDataPort, int eventPort = DefaultEventPort)
            : base(dataPort, eventPort)
        {
            _serverCryptographyHandler = new RSACryptographyHandler();

            if (serverPublicPrivateKey != null)
                _serverCryptographyHandler.PrivateKey = serverPublicPrivateKey;

            _clients = new Dictionary<TcpConnectedHost, EncryptedClientDataContainer>();

            Register(HandleRSAKeyExchangeRequest);
            Register(HandleAESKeyExchangeRequest);
            Register(HandleAESInitializationVectorExchangeRequest);
        }

        private TcpMessage HandleRSAKeyExchangeRequest(TcpConnectedHost host, TcpMessage message)
        {
            if (!message.HasType("rsakey"))
                return null;

            var client = GetClient(host);

            client.RSAHandler.PublicKey = message.Content;
            client.EncryptNextMessageAsync = false;

            return new TcpMessage("rsakey") { Content = _serverCryptographyHandler.PublicKey };
        }

        private TcpMessage HandleAESKeyExchangeRequest(TcpConnectedHost host, TcpMessage message)
        {
            if (!message.HasType("aeskey"))
                return null;

            var client = GetClient(host);
            client.AESHandler.GenerateKey();

            return new TcpMessage("aeskey") { Content = client.AESHandler.KeyAsString };
        }

        private TcpMessage HandleAESInitializationVectorExchangeRequest(TcpConnectedHost host, TcpMessage message)
        {
            if (!message.HasType("aesiv"))
                return null;

            var client = GetClient(host);
            client.AESHandler.GenerateInitializationVector();
            client.EncryptNextMessageSync = false;

            return new TcpMessage("aesiv") { Content = client.AESHandler.InitializationVectorAsString };
        }

        protected override string PrepareIncommingMessage(TcpConnectedHost host, string message)
        {
            var client = GetClient(host);

            if (client.AESHandler.IsReady)
                message = client.AESHandler.Decrypt(message);
            else if (client.RSAHandler.IsReady)
                message = _serverCryptographyHandler.Decrypt(message);

            return base.PrepareIncommingMessage(host, message);
        }

        protected override string PrepareOutgoingMessage(TcpConnectedHost host, string message)
        {
            var client = GetClient(host);

            if (client.AESHandler.IsReady && client.EncryptNextMessageSync)
                message = client.AESHandler.Encrypt(message);
            else if (client.RSAHandler.IsReady && client.EncryptNextMessageAsync)
                message = client.RSAHandler.Encrypt(message);

            if (!client.EncryptNextMessageSync)
                client.EncryptNextMessageSync = true;

            if (!client.EncryptNextMessageAsync)
                client.EncryptNextMessageAsync = true;

            return base.PrepareOutgoingMessage(host, message);
        }

        private EncryptedClientDataContainer GetClient(TcpConnectedHost host)
        {
            EncryptedClientDataContainer container;
            if (!_clients.TryGetValue(host, out container))
            {
                container = new EncryptedClientDataContainer();
                _clients.Add(host, container);
            }
            return container;
        }

        class EncryptedClientDataContainer
        {
            public RSACryptographyHandler RSAHandler { get; private set; }
            public AESCryptographyHandler AESHandler { get; private set; }
            public bool EncryptNextMessageSync { get; set; }
            public bool EncryptNextMessageAsync { get; set; }

            public EncryptedClientDataContainer()
            {
                RSAHandler = new RSACryptographyHandler();
                AESHandler = new AESCryptographyHandler();
                EncryptNextMessageSync = true;
                EncryptNextMessageAsync = true;
            }
        }
    }
}
