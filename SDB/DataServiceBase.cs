using System;
using System.Collections.Generic;

namespace SDB
{
    public abstract class DataServiceBase : IDisposable
    {
        public event Action<int> ItemChanged;
        public event Action<DbRelation> RelationAdded;
        public event Action<DbRelation> RelationRemoved;

        public abstract ICollection<DbRelation> GetRelations(int? fromId);
        public abstract DbRelation GetRelation(int? fromId, string identifier);

        public abstract DbItem GetItem(int id);

        public abstract void Insert(DbItem item);
        public abstract void Update(DbItem item);
        public abstract void Delete(DbItem item);

        public abstract void Insert(DbRelation relation);
        public abstract void Delete(DbRelation relation);

        public virtual DbItem GetItemByRelation(int? fromId, string identifier)
        {
            var relation = GetRelation(fromId, identifier);
            if (relation == null || relation.ToId == null)
                return null;

            return GetItem(relation.ToId.Value);
        }

        public virtual DbItem GetOrCreateItem(int? fromId, string identifier)
        {
            var relation = GetRelation(fromId, identifier);
            if (relation != null && relation.ToId == null)
            {
                Delete(relation);
                relation = null;
            }

            if (relation == null)
            {
                var item = new DbItem();

                Insert(item);

                Insert(new DbRelation
                                  {
                                      FromId = fromId,
                                      Identifier = identifier,
                                      ToId = item.Id
                                  });

                return item;
            }

            return GetItem(relation.ToId.Value);
        }

        protected void OnItemsChanged(ICollection<int> ids)
        {
            if (ids == null)
                return;

            foreach (var id in ids)
            {
                OnItemChanged(id);
            }
        }

        protected void OnItemChanged(int id)
        {
            if (ItemChanged != null)
                ItemChanged(id);
        }

        protected void OnRelationAdded(DbRelation relation)
        {
            if (RelationAdded != null)
                RelationAdded(relation);
        }

        protected void OnRelationRemoved(DbRelation relation)
        {
            if (RelationRemoved != null)
                RelationRemoved(relation);
        }

        public virtual void Dispose()
        {
        }
    }
}
