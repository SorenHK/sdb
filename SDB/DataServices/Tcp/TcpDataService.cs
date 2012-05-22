using System.Collections.Generic;
using System.Linq;

namespace SDB.DataServices.Tcp
{
    public class TcpDataService : DataServiceBase
    {
        public TcpClient Client { get; private set; }

        public TcpDataService(TcpClient client)
        {
            Client = client;

            Client.RegisterEventHandler(HandleItemChanged);
            Client.RegisterEventHandler(HandleRelationAdded);
            Client.RegisterEventHandler(HandleRelationRemoved);
        }

        private bool HandleItemChanged(TcpMessage request)
        {
            if (!request.HasType(TcpRequestType.List))
                return false;

            var items = new ObjectTcpMessage<ItemChangeEvent>(request).Items;
            if (items != null)
                OnItemsChanged(items.Select(i => i.Id).ToList());

            return true;
        }

        private bool HandleRelationAdded(TcpMessage request)
        {
            if (!request.HasType(TcpRequestType.InsertRelation))
                return false;

            var relation = new ObjectTcpMessage<DbRelation>(request).Item;
            if (relation != null)
                OnRelationAdded(relation);

            return true;
        }

        private bool HandleRelationRemoved(TcpMessage request)
        {
            if (!request.HasType(TcpRequestType.DeleteRelation))
                return false;

            var relation = new ObjectTcpMessage<DbRelation>(request).Item;
            if (relation != null)
                OnRelationRemoved(relation);

            return true;
        }

        public override ICollection<DbRelation> GetRelations(int? fromId)
        {
            var request = new ParamTcpMessage(TcpRequestType.MultiRelationQuery);
            request.SetParam("from_id", fromId);
            var response = Client.SendAndReceive<DbRelation>(request);
            return response.Items;
        }

        public override DbRelation GetRelation(int? fromId, string identifier)
        {
            var request = new ParamTcpMessage(TcpRequestType.UniqueRelationQuery);
            request.SetParam("from_id", fromId);
            request.SetParam("identifier", identifier);
            var response = Client.SendAndReceive<DbRelation>(request);
            return response.Item;
        }

        public override DbItem GetItem(int id)
        {
            var request = new ParamTcpMessage(TcpRequestType.UniqueItemQuery);
            request.SetParam("id", id);
            var response = Client.SendAndReceive<DbItem>(request);
            return response.Item;
        }

        public override void Insert(DbItem item)
        {
            var request = new ObjectTcpMessage<DbItem>(TcpRequestType.InsertItem);
            request.Add(item);
            var response = Client.SendAndReceive<DbItem>(request);
            var responseItem = response.Item;
            if (responseItem != null)
                item.Id = responseItem.Id;
        }

        public override void Update(DbItem item)
        {
            var request = new ObjectTcpMessage<DbItem>(TcpRequestType.UpdateItem);
            request.Add(item);
            Client.SendAndReceive(request);
        }

        public override void Delete(DbItem item)
        {
            var request = new ObjectTcpMessage<DbItem>(TcpRequestType.DeleteItem);
            request.Add(item);
            Client.SendAndReceive(request);
        }

        public override void Insert(DbRelation relation)
        {
            var request = new ObjectTcpMessage<DbRelation>(TcpRequestType.InsertRelation);
            request.Add(relation);
            var response = Client.SendAndReceive<DbRelation>(request);
            var responseRelation = response.Item;
            if (responseRelation != null)
                relation.Id = responseRelation.Id;
        }

        public override void Delete(DbRelation relation)
        {
            var request = new ObjectTcpMessage<DbRelation>(TcpRequestType.DeleteRelation);
            request.Add(relation);
            Client.SendAndReceive(request);
        }

        public override void Dispose()
        {
            if (Client != null)
                Client.Dispose();
        }
    }
}
