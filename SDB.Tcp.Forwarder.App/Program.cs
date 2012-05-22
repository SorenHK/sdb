using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SDB.DataServices.Cache;
using SDB.DataServices.Tcp;
using SDB.DataServices;

namespace SDB.Tcp.Forwarder.App
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

            DataServiceBase service = new TcpDataService(new TcpClient("home.sorenhk.dk"));
            service = new CacheDataService(service);

            var tcpServer = new TcpServer();

            var server = new TcpDataServiceServer(service);
            server.RegisterTo(tcpServer);

            Console.ReadLine();

            tcpServer.Dispose();
            service.Dispose();
        }
    }
}
