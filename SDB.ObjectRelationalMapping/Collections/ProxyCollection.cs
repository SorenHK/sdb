using System;
using System.Collections.Specialized;
using System.ComponentModel;
using SDB.DataServices;
using SDB.ObjectRelationalMapping.Proxy;

namespace SDB.ObjectRelationalMapping.Collections
{
    class ProxyCollection<T> : ThreadedBindingList<T>, IProxyCollection<T>
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private readonly ObjectMapper _objectMapper;
        private readonly int? _parentId;
        private bool _isLoaded;
        private readonly Type _proxyType;
        private readonly object _lockObject;
        private readonly bool _manageRelations;

        public ProxyCollection(ObjectMapper objectMapper, int? parentId, bool manageRelations = false)
        {
            _lockObject = new object();
            _objectMapper = objectMapper;
            _parentId = parentId;
            _manageRelations = manageRelations;
            _proxyType = ProxyMapper.GetProxyType(typeof(T));

            _objectMapper.DataService.RelationAdded += OnRelationAdded;
            _objectMapper.DataService.RelationRemoved += OnRelationRemoved;

            Load();
        }

        private void OnRelationAdded(DbRelation relation)
        {
            if (relation == null || relation.ToId == null || relation.FromId != _parentId)
                return;

            lock (_lockObject)
            {
                if (Contains(relation.ToId.Value))
                    return;

                base.Add(ProxyMapper.New<T>(relation.ToId.Value, _objectMapper, _proxyType));
            }
        }

        private void OnRelationRemoved(DbRelation relation)
        {
            if (relation == null || relation.ToId == null || relation.FromId != _parentId)
                return;

            lock (_lockObject)
            {
                var index = IndexOf(relation.ToId.Value);
                if (index >= 0)
                    RemoveAt(index);
            }
        }

        internal void Load()
        {
            if (_isLoaded)
                return;

            lock (_lockObject)
            {
                var relations = _objectMapper.DataService.GetRelations(_parentId);
                if (relations != null)
                {
                    foreach (var relation in relations)
                    {
                        if (relation.ToId == null)
                            continue;

                        base.Add(ProxyMapper.New<T>(relation.ToId.Value, _objectMapper, _proxyType));
                    }
                }
            }

            _isLoaded = true;
        }

        public void Add(ref T item)
        {
            item = Add(item);
        }

        public new T Add(T item)
        {
            item = PrepareForInsert(item);
            base.Add(item);

            return item;
        }

        public new void Insert(int index, T item)
        {
            base.Insert(index, PrepareForInsert(item));
        }

        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, PrepareForInsert(item));
        }

        protected override void SetItem(int index, T item)
        {
            base.SetItem(index, PrepareForInsert(item));
        }

        protected override void RemoveItem(int index)
        {
            var item = this[index];

            lock (_lockObject)
            {
                ObjectMapper.Remove(item, _parentId, _objectMapper.DataService);

                base.RemoveItem(index);
            }
        }

        protected override void ClearItems()
        {
            lock (_lockObject)
            {
                foreach(var item in this)
                {
                    ObjectMapper.Remove(item, _parentId, _objectMapper.DataService);
                }
            }

            base.ClearItems();
        }

        private T PrepareForInsert(T item)
        {
            if (!_proxyType.IsInstanceOfType(item))
            {
                lock (_lockObject)
                {
                    item = _objectMapper.Save(item);
                }
            }

            if (_isLoaded && _manageRelations)
            {
                lock (_lockObject)
                {
                    _objectMapper.DataService.Insert(new DbRelation(_parentId, "item", (item as IProxy).SDBId, DbRelationType.Relation));
                }
            }

            return item;
        }

        protected override void OnListChanged(ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemAdded)
            {
                var item = this[e.NewIndex] as INotifyPropertyChanged;
                if (item != null)
                    item.PropertyChanged += EntityPropertyChanged;
            }

            base.OnListChanged(e);
        }

        private void EntityPropertyChanged(object obj, PropertyChangedEventArgs e)
        {
            var index = IndexOf((T) obj);
            if (index < 0)
            {
                var item = obj as INotifyPropertyChanged;
                if (item != null)
                    item.PropertyChanged -= EntityPropertyChanged;
                return;
            }

            OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
        }

        private bool Contains(int id)
        {
            return IndexOf(id) >= 0;
        }

        private int IndexOf(int id)
        {
            for (var i = 0; i < Count; i++)
            {
                var item = this[i];
                if (!(item is IProxy)) 
                    continue;

                if ((item as IProxy).SDBId == id)
                    return i;
            }
            return -1;
        }
    }
}
