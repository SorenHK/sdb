using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace SDB.DataServices.Tcp
{
    class TcpProtocolHelper
    {
        private const int HeaderLength = 4;
        private const int BufferSize = 4096;
        public static Encoding Encoding = new UTF8Encoding();

        public static string Read(Stream stream)
        {
            var buffer = new byte[BufferSize];

            try
            {
                // Read header
                var bytesRead = stream.Read(buffer, 0, HeaderLength);

                if (bytesRead != 4)
                    return null;

                var contentSize = BitConverter.ToInt32(buffer, 0);
                var content = new byte[contentSize];

                var totalBytesRead = 0;
                do
                {
                    int readSize = contentSize - totalBytesRead;
                    if (readSize > BufferSize)
                        readSize = BufferSize;
                    bytesRead = stream.Read(buffer, 0, readSize);
                    Array.Copy(buffer, 0, content, totalBytesRead, bytesRead);
                } while ((totalBytesRead += bytesRead) < contentSize);

                return Encoding.GetString(content, 0, totalBytesRead);
            }
            catch (IOException)
            {
                return null;
            }
        }

        public static bool Write(Stream stream, TcpMessage message)
        {
            return Write(stream, message.ToString());
        }

        public static bool Write(Stream stream, string message)
        {
            var buffer = BuildMessage(message);

            try
            {
                stream.Write(buffer, 0, buffer.Length);
                stream.Flush();
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public static byte[] BuildMessage(string message)
        {
            var body = Encoding.GetBytes(message);

            var header = BitConverter.GetBytes(body.Length);

            return header.Concat(body).ToArray();
        }
    }
}
