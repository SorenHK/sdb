using System;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;

namespace SDB.Helpers
{
    public class RSACryptographyHandler
    {
        private readonly RSACryptoServiceProvider _provider;

        public bool IsReady { get; set; }

        public RSACryptographyHandler()
        {
            _provider = new RSACryptoServiceProvider();
        }

        public string PublicKey
        {
            get
            {
                IsReady = true;
                return _provider.ToXmlString(false);
            }
            set
            {
                _provider.FromXmlString(value);
                IsReady = true;
            }
        }

        public string PrivateKey
        {
            get
            {
                IsReady = true;
                return _provider.ToXmlString(true);
            }
            set
            {
                _provider.FromXmlString(value);
                IsReady = true;
            }
        }

        public string Encrypt(string message)
        {
            var plainBytes = Encoding.UTF8.GetBytes(message);
            var cipherBytes = Encrypt(plainBytes);
            return Convert.ToBase64String(cipherBytes);
        }

        public byte[] Encrypt(byte[] message)
        {
            if (!IsReady)
                IsReady = true;

            return _provider.Encrypt(message, false);
        }

        public string Decrypt(string message)
        {
            var cipherBytes = Convert.FromBase64String(message);
            var plainBytes = Decrypt(cipherBytes);
            return Encoding.UTF8.GetString(plainBytes);
        }

        public byte[] Decrypt(byte[] message)
        {
            if (!IsReady)
                IsReady = true;

            return _provider.Decrypt(message, false);
        }
    }
}
