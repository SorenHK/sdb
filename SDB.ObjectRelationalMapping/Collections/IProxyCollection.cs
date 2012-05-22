using System.Collections.Generic;
using System.Collections.Specialized;

namespace SDB.ObjectRelationalMapping.Collections
{
    public interface IProxyCollection<T> : IList<T>, INotifyCollectionChanged
    {
        void Add(ref T item);
        new T Add(T item);
    }
}
