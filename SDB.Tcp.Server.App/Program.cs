using System;
using System.Diagnostics;
using SDB.DataServices.Cache;
using SDB.DataServices.MySQL;
using SDB.DataServices.Tcp;

namespace SDB.Tcp.Server.App
{
    class Program
    {
        static void Main(string[] args)
        {
            Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

            var dataSource = new CacheDataService(new MySQLDataService());
            //var dataSource = new MemoryDataService();

            var server = new EncryptedTcpServer(storeKeysInConfiguration: true);
            server.AllowAll = true;
            
            var auth = new TcpBasicAuthenticationProvider(dataSource);
            auth.AutoRegisterUsers = true;
            auth.RegisterHandlersTo(server);

            var serviceServer = new TcpDataServiceServer(dataSource);
            serviceServer.RegisterTo(server);
            serviceServer.RegisterAuthProvider(auth);

            Console.ReadLine();

            server.Dispose();
            dataSource.Dispose();
        }
    }
}
