using System;
using SDB.ObjectRelationalMapping.Collections;
using SDB.ObjectRelationalMapping.Proxy;

namespace SDB.ObjectRelationalMapping
{
    public class ObjectMapper : IDisposable
    {
        private const string ClassesIdentifier = "sdb_classes";

        private readonly int? _parentId;
        private int? _classesParentId;

        internal DataServiceBase DataService { get; private set; }

        internal int ClassesParentId
        {
            get
            {
                if (_classesParentId == null)
                {
                    _classesParentId = DataService.GetOrCreateItem(_parentId, ClassesIdentifier).Id;
                }
                return _classesParentId.Value;
            }
        }

        public ObjectMapper(DataServiceBase dataService, int? parentId = null)
        {
            DataService = dataService;
            _parentId = parentId;
        }

        public T GetSingle<T>(int id)
        {
            return (T)GetSingle(typeof(T), id);
        }

        public object GetSingle(Type type, int id)
        {
            return ProxyMapper.New(type, id, this);
        }

        public IProxyCollection<T> Get<T>()
        {
            return new ProxyCollection<T>(this, GetObjectsContainerItemForType(typeof(T)).Id);
        }

        public object Save(object obj)
        {
            if (obj == null || ProxyMapper.IsProxy(obj))
                return obj;

            var type = obj.GetType();

            var container = GetObjectsContainerItemForType(type);

            var item = new DbItem();
            DataService.Insert(item);

            var relation = new DbRelation
            {
                FromId = container.Id,
                Identifier = type.Name, // This can be anything, really...
                ToId = item.Id
            };
            DataService.Insert(relation);

            return ProxyMapper.Save(obj, item.Id, this);
        }

        public T Save<T>(T obj)
        {
            return (T)Save((object)obj);
        }

        public void Delete<T>(T obj) where T : class
        {
            if (obj == null)
                return;

            var container = GetObjectsContainerItemForType(obj.GetType());

            Remove(obj, container.Id, DataService);
        }

        private DbItem GetObjectsContainerItemForType(Type type)
        {
            var classItem = DataService.GetOrCreateItem(ClassesParentId, type.Name);
            return DataService.GetOrCreateItem(classItem.Id, "objects");
        }

        public void Dispose()
        {
            if (DataService != null)
                DataService.Dispose();
        }

        public static void Remove<T>(T obj, int? fromId, DataServiceBase dataService)
        {
            var proxy = obj as IProxy;
            if (proxy == null)
                return;

            var relations = dataService.GetRelations(fromId);
            foreach (var relation in relations)
            {
                if (relation.ToId == proxy.SDBId)
                {
                    dataService.Delete(relation);
                }
            }
        }
    }
}
