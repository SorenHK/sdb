using System;
using System.Collections;
using SDB.ObjectRelationalMapping.Collections;

namespace SDB.ObjectRelationalMapping.Proxy
{
    public class CollectionPropertyLoadHandler<T, S> : PropertyLoadHandlerBase<T> where T : IEnumerable
    {
        private IProxyCollection<S> _children;

        public CollectionPropertyLoadHandler(IProxy proxy, string propertyName, ObjectMapper objectMapper)
            : base(proxy, propertyName, objectMapper)
        {
        }

        protected override T GetValue(DbItem item)
        {
            _children = new ProxyCollection<S>(ObjectMapper, item.Id, true);
            return (T)_children;
        }

        protected override void SetValue(ref T value, DbItem item)
        {
            throw new InvalidOperationException("Assignment to collection properties is currently not supported");
        }
    }
}
