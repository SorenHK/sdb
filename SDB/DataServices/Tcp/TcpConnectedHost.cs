using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDB.DataServices.Tcp
{
    public class TcpConnectedHost
    {
        public System.Net.Sockets.TcpClient TcpClient { get; set; }
        public string IPAddress { get; set; }
    }
}
