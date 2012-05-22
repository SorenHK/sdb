using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SDB.Helpers
{
    public class HashHelper
    {
        public static string GenerateSaltedHash(string plainText, string salt)
        {
            return GenerateSaltedHash(plainText, salt, new SHA256Managed());
        }

        public static string GenerateSaltedHash(string plainText, string salt, HashAlgorithm algorithm)
        {
            var bytesPlainText = Encoding.UTF8.GetBytes(plainText);
            var bytesSalt = Convert.FromBase64String(salt);

            return Convert.ToBase64String(GenerateSaltedHash(bytesPlainText, bytesSalt, algorithm));
        }

        public static byte[] GenerateSaltedHash(byte[] plainText, byte[] salt)
        {
            return GenerateSaltedHash(plainText, salt, new SHA256Managed());
        }

        public static byte[] GenerateSaltedHash(byte[] plainText, byte[] salt, HashAlgorithm algorithm)
        {
            if (algorithm == null)
                throw new ArgumentNullException("algorithm");

            var plainTextWithSaltBytes = new byte[plainText.Length + salt.Length];

            for (var i = 0; i < plainText.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainText[i];
            }

            for (var i = 0; i < salt.Length; i++)
            {
                plainTextWithSaltBytes[plainText.Length + i] = salt[i];
            }

            return algorithm.ComputeHash(plainTextWithSaltBytes);
        }

        public static bool ConfirmPassword(string storedPasswordHash, string password, string salt)
        {
            return ConfirmPassword(storedPasswordHash, password, salt, new SHA256Managed());
        }

        public static bool ConfirmPassword(string storedPasswordHash, string password, string salt, HashAlgorithm algorithm)
        {
            var passwordHash = GenerateSaltedHash(password, salt, algorithm);

            return storedPasswordHash.SequenceEqual(passwordHash);
        }

        public static bool ConfirmPassword(byte[] storedPasswordHash, byte[] password, byte[] salt)
        {
            return ConfirmPassword(storedPasswordHash, password, salt, new SHA256Managed());
        }

        public static bool ConfirmPassword(byte[] storedPasswordHash, byte[] password, byte[] salt, HashAlgorithm algorithm)
        {
            var passwordHash = GenerateSaltedHash(password, salt, algorithm);

            return storedPasswordHash.SequenceEqual(passwordHash);
        }

        public static string CreateSaltString(int size)
        {
            return Convert.ToBase64String(CreateSaltBytes(size));
        }

        public static byte[] CreateSaltBytes(int size)
        {
            //Generate a cryptographic random number.
            var rng = new RNGCryptoServiceProvider();
            var buffer = new byte[size];
            rng.GetBytes(buffer);

            // Return a Base64 string representation of the random number.
            return buffer;
        }
    }
}
