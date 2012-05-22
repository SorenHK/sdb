using System.Collections.Generic;
using System.Linq;
using SDB.DataServices.Auth;

namespace SDB.DataServices.Tcp
{
    public class TcpDataServiceServer
    {
        private readonly DataServiceBase _dataService;
        private readonly LinkedList<ITcpAuthenticationProvider> _authenticationProviders;

        public TcpDataServiceServer(DataServiceBase dataService, TcpServer server)
            : this(dataService)
        {
            if (server == null)
                server = new TcpServer();

            RegisterTo(server);
        }

        public TcpDataServiceServer(DataServiceBase dataService)
        {
            _dataService = dataService;
            _authenticationProviders = new LinkedList<ITcpAuthenticationProvider>();
        }

        public void RegisterAuthProvider(ITcpAuthenticationProvider authenticationProvider)
        {
            _authenticationProviders.AddLast(authenticationProvider);
        }

        private bool IsAuthenticated(TcpConnectedHost host)
        {
            return _authenticationProviders.Any(provider => provider.IsAuthenticated(host));
        }

        //private DbItem GetUserItem(TcpConnectedHost host)
        //{
        //    return _authenticationProviders.Where(p => p.IsAuthenticated(host)).Select(p => p.GetUserItem(host)).FirstOrDefault();
        //}

        public void RegisterTo(TcpServer server)
        {
            _dataService.ItemChanged += delegate(int id)
            {
                var message = new ObjectTcpMessage<ItemChangeEvent>(TcpRequestType.List);
                message.Add(new ItemChangeEvent { Id = id });
                server.Enqueue(message);
            };

            _dataService.RelationAdded += delegate(DbRelation relation)
            {
                var message = new ObjectTcpMessage<DbRelation>(TcpRequestType.InsertRelation);
                message.Add(relation);
                server.Enqueue(message);
            };

            _dataService.RelationRemoved += delegate(DbRelation relation)
            {
                var message = new ObjectTcpMessage<DbRelation>(TcpRequestType.DeleteRelation);
                message.Add(relation);
                server.Enqueue(message);
            };

            server.Register(HandleMultiRelationQuery);
            server.Register(HandleUniqueRelationQuery);
            server.Register(HandleInsertRelation);
            server.Register(HandleDeleteRelation);
            server.Register(HandleUniqueItemQuery);
            server.Register(HandleInsertItem);
            server.Register(HandleUpdateItem);
            server.Register(HandleDeleteItem);
        }

        private TcpMessage HandleMultiRelationQuery(TcpConnectedHost host, TcpMessage message)
        {
            if (!message.HasType(TcpRequestType.MultiRelationQuery))
                return null;

            if (!IsAuthenticated(host))
                throw AuthException.NotLoggedIn();

            var request = new ParamTcpMessage(message);

            var response = new ObjectTcpMessage<DbRelation>(TcpRequestType.List);

            if (request.HasParam("from_id"))
            {
                var fromId = request.GetParamAsNullableInt("from_id");
                var items = _dataService.GetRelations(fromId);
                response.Add(items);
                return response;
            }

            return TcpMessage.Error("Missing or badly formatted query parameters");
        }

        private TcpMessage HandleUniqueRelationQuery(TcpConnectedHost host, TcpMessage message)
        {
            if (!message.HasType(TcpRequestType.UniqueRelationQuery))
                return null;

            if (!IsAuthenticated(host))
                throw AuthException.NotLoggedIn();

            var request = new ParamTcpMessage(message);

            var response = new ObjectTcpMessage<DbRelation>(TcpRequestType.List);
            if (request.HasParam("from_id") && request.HasParam("identifier"))
            {
                var fromId = request.GetParamAsNullableInt("from_id");
                var identifier = request.GetParam("identifier");
                var relation = _dataService.GetRelation(fromId, identifier);
                response.Add(relation);
                return response;
            }

            return TcpMessage.Error("Missing or badly formatted query parameters");
        }

        private TcpMessage HandleInsertRelation(TcpConnectedHost host, TcpMessage message)
        {
            if (!message.HasType(TcpRequestType.InsertRelation))
                return null;

            if (!IsAuthenticated(host))
                throw AuthException.NotLoggedIn();

            var request = new ObjectTcpMessage<DbRelation>(message);

            var response = new ObjectTcpMessage<DbRelation>(TcpRequestType.List);
            var relation = request.Item;
            if (relation != null)
            {
                _dataService.Insert(relation);
                response.Add(relation); // Send the item back to report assigned Id
            }

            return response;
        }

        private TcpMessage HandleDeleteRelation(TcpConnectedHost host, TcpMessage message)
        {
            if (!message.HasType(TcpRequestType.DeleteRelation))
                return null;

            if (!IsAuthenticated(host))
                throw AuthException.NotLoggedIn();

            var request = new ObjectTcpMessage<DbRelation>(message);

            var relation = request.Item;
            if (relation != null)
            {
                _dataService.Delete(relation);
            }

            return new TcpMessage(TcpRequestType.Ok);
        }

        private TcpMessage HandleUniqueItemQuery(TcpConnectedHost host, TcpMessage message)
        {
            if (!message.HasType(TcpRequestType.UniqueItemQuery))
                return null;

            if (!IsAuthenticated(host))
                throw AuthException.NotLoggedIn();

            var request = new ParamTcpMessage(message);

            var response = new ObjectTcpMessage<DbItem>(TcpRequestType.List);
            if (request.HasParam("id"))
            {
                var id = request.GetParamAsNullableInt("id");
                if (id != null)
                {
                    var item = _dataService.GetItem(id.Value);
                    response.Add(item);
                    return response;
                }
            }

            return TcpMessage.Error("Missing or badly formatted query parameters");
        }

        private TcpMessage HandleInsertItem(TcpConnectedHost host, TcpMessage message)
        {
            if (!message.HasType(TcpRequestType.InsertItem))
                return null;

            if (!IsAuthenticated(host))
                throw AuthException.NotLoggedIn();

            var request = new ObjectTcpMessage<DbItem>(message);

            var response = new ObjectTcpMessage<DbItem>(TcpRequestType.List);
            var item = request.Item;
            if (item != null)
            {
                _dataService.Insert(item);
                response.Add(item); // Send the item back to report assigned Id
            }

            return response;
        }

        private TcpMessage HandleUpdateItem(TcpConnectedHost host, TcpMessage message)
        {
            if (!message.HasType(TcpRequestType.UpdateItem))
                return null;

            if (!IsAuthenticated(host))
                throw AuthException.NotLoggedIn();

            var request = new ObjectTcpMessage<DbItem>(message);

            var item = request.Item;
            if (item != null)
            {
                _dataService.Update(item);
            }

            return new TcpMessage(TcpRequestType.Ok);
        }

        private TcpMessage HandleDeleteItem(TcpConnectedHost host, TcpMessage message)
        {
            if (!message.HasType(TcpRequestType.DeleteItem))
                return null;

            if (!IsAuthenticated(host))
                throw AuthException.NotLoggedIn();

            var request = new ObjectTcpMessage<DbItem>(message);

            var item = request.Item;
            if (item != null)
            {
                _dataService.Delete(item);
            }

            return new TcpMessage(TcpRequestType.Ok);
        }
    }
}
