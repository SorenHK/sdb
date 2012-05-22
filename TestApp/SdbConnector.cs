using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SDB;
using SDB.DataServices.Auth;
using SDB.DataServices.Cache;
using SDB.DataServices.Tcp;
using SDB.ObjectRelationalMapping;

namespace TestApp
{
    public class SdbConnector : IDisposable
    {
        public string Name { get; private set; }
        public ObjectMapper ObjectMapper { get; private set; }
        public DataServiceBase DataService { get; private set; }

        public SdbConnector(string host, bool cache)
        {
            Name = host;

            var client = new EncryptedTcpClient(host);

            //_service = new MysqlDataService("Server=localhost;Database=sdb;User ID=root;CharSet=utf8");
            //_service = new MemoryDataService();
            DataService = new TcpDataService(client);

            if (cache)
                DataService = new CacheDataService(DataService);

            var authenticator = new TcpBasicClientAuthenticator(DataService, client);

            DataService = new AuthDataService(DataService, authenticator);

            try
            {
                authenticator.Login("sorenhk", "abc");
            }
            catch (AuthException e)
            {
                MessageBox.Show(e.Message);
                return;
            }

            ObjectMapper = new ObjectMapper(DataService, authenticator.UserWorkspaceContainerId);
        }

        public void Dispose()
        {
            if (DataService != null)
                DataService.Dispose();
        }
    }
}
