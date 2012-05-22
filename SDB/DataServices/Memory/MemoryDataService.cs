using System;
using System.Collections.Generic;
using System.Linq;
using SDB.Helpers;

namespace SDB.DataServices.Memory
{
    public class MemoryDataService : DataServiceBase
    {
        private readonly IdGenerator _itemIdGenerator;
        private readonly IdGenerator _relationIdGenerator;
        private readonly LinkedList<DbItem> _items;
        private readonly LinkedList<DbRelation> _relations; 
        private readonly object _lockObject;

        public MemoryDataService()
        {
            _itemIdGenerator = new IdGenerator();
            _relationIdGenerator = new IdGenerator();
            _items = new LinkedList<DbItem>();
            _relations = new LinkedList<DbRelation>();
            _lockObject = new object();
        }

        public override ICollection<DbRelation> GetRelations(int? fromId)
        {
            lock (_lockObject)
            {
                return _relations.Where(i => i.FromId == fromId).ToList();
            }
        }

        public override DbRelation GetRelation(int? fromId, string identifier)
        {
            lock (_lockObject)
            {
                return _relations.FirstOrDefault(i => i.FromId == fromId && i.Identifier == identifier);
            }
        }

        public override DbItem GetItem(int id)
        {
            lock (_lockObject)
            {
                return _items.FirstOrDefault(i => i.Id == id);
            }
        }

        public override void Insert(DbItem item)
        {
            lock (_lockObject)
            {
                if (item.Id == 0)
                    item.Id = _itemIdGenerator.Get();

                _items.AddLast(item);
            }
        }

        public override void Update(DbItem item)
        {
            lock(_lockObject)
            {
                var itemInList = _items.FirstOrDefault(i => i.Id == item.Id);
                if (itemInList == null)
                    return;

                itemInList.Value = item.Value;
                itemInList.RefId = item.RefId;

                OnItemChanged(item.Id);
            }
        }

        public override void Delete(DbItem item)
        {
            lock (_lockObject)
            {
                var itemInList = _items.FirstOrDefault(i => i.Id == item.Id);
                if (itemInList == null)
                    return;

                _items.Remove(itemInList);
            }
        }

        public override void Insert(DbRelation relation)
        {
            lock (_lockObject)
            {
                if (relation.Id == 0)
                    relation.Id = _relationIdGenerator.Get();

                _relations.AddLast(relation);
            }
            OnRelationAdded(relation);
        }

        public override void Delete(DbRelation relation)
        {
            lock (_lockObject)
            {
                var relationInList = _relations.FirstOrDefault(i => i.Id == relation.Id);
                if (relationInList == null)
                    return;

                _relations.Remove(relationInList);
            }
            OnRelationRemoved(relation);
        }
    }
}
