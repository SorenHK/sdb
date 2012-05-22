using System.Collections.Generic;
using System.Linq;

namespace SDB.DataServices.Cache
{
    public class CacheDataService : DataServiceBase
    {
        private readonly DataServiceBase _dataService;
        private readonly Dictionary<int, DbItem> _items;
        private CacheRelationList _rootRelations;
        private readonly Dictionary<int, CacheRelationList> _relations;
        private readonly object _lockObj;

        public CacheDataService(DataServiceBase dataService)
        {
            _dataService = dataService;
            _items = new Dictionary<int, DbItem>();
            _relations = new Dictionary<int, CacheRelationList>();
            _lockObj = new object();

            _dataService.ItemChanged += DataServiceItemChanged;
            _dataService.RelationAdded += OnRelationAdded;
            _dataService.RelationRemoved += OnRelationRemoved;
        }

        private DbItem GetCacheItem(int id)
        {
            DbItem item;
            _items.TryGetValue(id, out item);
            if (item != null)
                return item;

            item = _dataService.GetItem(id);
            if (item != null)
            {
                _items[id] = item;
            }
            return item;
        }

        private void ClearRelationCache(int? fromId)
        {
            if (fromId == null)
                _rootRelations = null;
            else
                _relations.Remove(fromId.Value);
        }

        private LinkedList<DbRelation> GetCacheRelationsByParent(int? fromId)
        {
            CacheRelationList list;

            if (fromId == null)
                list = _rootRelations;
            else
                _relations.TryGetValue(fromId.Value, out list);

            if (list == null)
            {
                list = new CacheRelationList
                       {
                           FromId = fromId
                       };

                var relationsFromDb = _dataService.GetRelations(fromId);
                if (relationsFromDb != null)
                {
                    foreach (var relation in relationsFromDb)
                    {
                        list.AddLast(relation);
                    }
                }

                if (fromId == null)
                    _rootRelations = list;
                else
                    _relations[fromId.Value] = list;
            }

            return list;
        }

        private DbRelation GetCacheRelation(int? fromId, string identifier)
        {
            var list = GetCacheRelationsByParent(fromId);
            return list.FirstOrDefault(r => r.Identifier.Equals(identifier));
        }

        private void DataServiceItemChanged(int id)
        {
            lock (_lockObj)
            {
                if (_items.ContainsKey(id))
                    _items[id] = _dataService.GetItem(id);

                OnItemChanged(id);
            }
        }

        public override ICollection<DbRelation> GetRelations(int? fromId)
        {
            lock (_lockObj)
            {
                return GetCacheRelationsByParent(fromId);
            }
        }

        public override DbRelation GetRelation(int? fromId, string identifier)
        {
            lock (_lockObj)
            {
                return GetCacheRelation(fromId, identifier);
            }
        }

        public override DbItem GetItem(int id)
        {
            lock (_lockObj)
            {
                return GetCacheItem(id);
            }
        }

        public override DbItem GetItemByRelation(int? fromId, string identifier)
        {
            lock (_lockObj)
            {
                var relation = GetCacheRelation(fromId, identifier);
                return relation != null && relation.ToId != null ? GetCacheItem(relation.ToId.Value) : null;
            }
        }

        public override void Insert(DbItem item)
        {
            lock (_lockObj)
            {
                _dataService.Insert(item);
                _items[item.Id] = item;
            }
        }

        public override void Update(DbItem item)
        {
            lock (_lockObj)
            {
                _dataService.Update(item);
                _items[item.Id] = item;
            }
        }

        public override void Delete(DbItem item)
        {
            lock (_lockObj)
            {
                _dataService.Delete(item);
                _items[item.Id] = null;
            }
        }

        public override void Insert(DbRelation relation)
        {
            lock (_lockObj)
            {
                _dataService.Insert(relation);
                var list = GetCacheRelationsByParent(relation.FromId);
                list.AddLast(relation);
            }
        }

        public override void Delete(DbRelation relation)
        {
            lock (_lockObj)
            {
                _dataService.Delete(relation);
                ClearRelationCache(relation.FromId);
            }
        }

        public override void Dispose()
        {
            if (_dataService != null)
            {
                _dataService.ItemChanged -= DataServiceItemChanged;
                _dataService.Dispose();
            }
        }

        private class CacheRelationList : LinkedList<DbRelation>
        {
            public int? FromId { get; set; }
        }
    }
}
