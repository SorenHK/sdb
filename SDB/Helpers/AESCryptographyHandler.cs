using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SDB.Helpers
{
    public class AESCryptographyHandler
    {
        private readonly AesManaged _provider;
        private ICryptoTransform _decryptor;
        private ICryptoTransform _encryptor;

        public AESCryptographyHandler()
        {
            _provider = new AesManaged();
            _provider.Mode = CipherMode.CBC;
            _provider.FeedbackSize = 8;
        }

        public bool HasKey { get; private set; }
        public bool HasInitializationVector { get; private set; }
        public bool IsReady { get { return HasKey && HasInitializationVector; } }

        public string KeyAsString
        {
            get { return Convert.ToBase64String(Key); }
            set { Key = Convert.FromBase64String(value); }
        }

        public byte[] Key
        {
            get { return _provider.Key; }
            set
            {
                _provider.Key = value;
                HasKey = true;
            }
        }

        public string InitializationVectorAsString
        {
            get { return Convert.ToBase64String(InitializationVector); }
            set { InitializationVector = Convert.FromBase64String(value); }
        }

        public byte[] InitializationVector
        {
            get { return _provider.IV; }
            set
            {
                _provider.IV = value;
                HasInitializationVector = true;
            }
        }

        private void Init()
        {
            GenerateKey();
            GenerateInitializationVector();

            if (_encryptor == null)
                _encryptor = _provider.CreateEncryptor();

            if (_decryptor == null)
                _decryptor = _provider.CreateDecryptor();
        }

        public void GenerateKey()
        {
            if (HasKey) 
                return;

            _provider.GenerateKey();
            HasKey = true;
        }

        public void GenerateInitializationVector()
        {
            if (HasInitializationVector)
                return;

            _provider.GenerateIV();
            HasInitializationVector = true;
        }

        public string Encrypt(string message)
        {
            var plainBytes = Encoding.UTF8.GetBytes(message);
            var cipherBytes = Encrypt(plainBytes);
            var result = Convert.ToBase64String(cipherBytes);
            return result;
        }

        public byte[] Encrypt(byte[] message)
        {
            Init();

            var msEncrypt = new MemoryStream();

            using (var csEncrypt = new CryptoStream(msEncrypt, _encryptor, CryptoStreamMode.Write))
            {
                csEncrypt.Write(message, 0, message.Length);
            }

            return msEncrypt.ToArray();
        }

        public string Decrypt(string message)
        {
            var cipherBytes = Convert.FromBase64String(message);
            var plainBytes = Decrypt(cipherBytes);
            var result = Encoding.UTF8.GetString(plainBytes);
            return result;
        }

        public byte[] Decrypt(byte[] message)
        {
            Init();

            var msDecrypt = new MemoryStream();

            using (var csDecrypt = new CryptoStream(msDecrypt, _decryptor, CryptoStreamMode.Write))
            {
                csDecrypt.Write(message, 0, message.Length);
            }

            return msDecrypt.ToArray();
        }
    }
}
