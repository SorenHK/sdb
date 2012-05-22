using System.Collections.Generic;
using SDB.Helpers;

namespace SDB.DataServices.Tcp
{
    public class TcpBasicAuthenticationProvider : ITcpAuthenticationProvider
    {
        private const string UsersIdentifier = "sdb_users";

        private readonly DataServiceBase _dataService;
        private readonly int? _parentId;
        private readonly Dictionary<TcpConnectedHost, DbItem> _hostUsers; 

        public bool AutoRegisterUsers { get; set; }

        private int? _usersParentId;
        internal int UsersParentId
        {
            get
            {
                if (_usersParentId == null)
                    _usersParentId = _dataService.GetOrCreateItem(_parentId, UsersIdentifier).Id;
                return _usersParentId.Value;
            }
        }

        public TcpBasicAuthenticationProvider(DataServiceBase dataService, TcpServer server, int? parentId = null)
            : this(dataService, parentId)
        {
            RegisterHandlersTo(server);
        }

        public TcpBasicAuthenticationProvider(DataServiceBase dataService, int? parentId = null)
        {
            _dataService = dataService;
            _parentId = parentId;
            _hostUsers = new Dictionary<TcpConnectedHost, DbItem>();
        }

        public bool IsAuthenticated(TcpConnectedHost host)
        {
            return GetUserItem(host) != null;
        }

        public DbItem GetUserItem(TcpConnectedHost host)
        {
            DbItem userItem;
            _hostUsers.TryGetValue(host, out userItem);
            return userItem;
        }

        private TcpMessage HandleLoginRequest(TcpConnectedHost host, TcpMessage message)
        {
            if (!message.HasType("loginbasic"))
                return null;

            var request = new ParamTcpMessage(message);

            var username = request.GetParam("username");
            var password = request.GetParam("password");

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                var userItem = _dataService.GetItemByRelation(UsersParentId, username);

                if (userItem == null)
                {
                    if (!AutoRegisterUsers)
                        return TcpMessage.Error("User not found. Automatic registering of users is currently disabled.");

                    // Register
                    userItem = new DbItem(username);
                    _dataService.Insert(userItem);
                    _dataService.Insert(new DbRelation(UsersParentId, username, userItem.Id));

                    var saltItem = new DbItem(HashHelper.CreateSaltString(10));
                    _dataService.Insert(saltItem);
                    _dataService.Insert(new DbRelation(userItem.Id, "salt", saltItem.Id));

                    var passwordItem = new DbItem(HashHelper.GenerateSaltedHash(password, saltItem.Value));
                    _dataService.Insert(passwordItem);
                    _dataService.Insert(new DbRelation(userItem.Id, "password", passwordItem.Id));
                }
                else
                {
                    // Login
                    var saltItem = _dataService.GetItemByRelation(userItem.Id, "salt");
                    var passwordItem = _dataService.GetItemByRelation(userItem.Id, "password");

                    if (saltItem == null || saltItem.Value == null || passwordItem == null || passwordItem.Value == null || !HashHelper.ConfirmPassword(passwordItem.Value, password, saltItem.Value))
                        return TcpMessage.Error("Login failed. Wrong password.");
                }

                _hostUsers[host] = userItem;

                var response = new ObjectTcpMessage<DbItem>(TcpRequestType.Ok);
                response.Add(userItem);
                return response;
            }

            return TcpMessage.Error("Missing or badly formatted login parameters");
        }

        public void RegisterHandlersTo(TcpServer server)
        {
            if (server == null)
                return;

            server.Register(HandleLoginRequest);
        }

        public void RegisterAuthProviderTo(TcpDataServiceServer server)
        {
            if (server == null)
                return;

            server.RegisterAuthProvider(this);
        }
    }
}
