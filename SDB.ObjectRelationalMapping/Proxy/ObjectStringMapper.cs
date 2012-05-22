namespace SDB.ObjectRelationalMapping.Proxy
{
    public abstract class ObjectStringMapper<T> : IObjectStringMapper
    {
        public object FromString(string value)
        {
            return ObjectFromString(value);
        }

        public string ToString(object value)
        {
            return ObjectToString((T) value);
        }

        protected abstract T ObjectFromString(string value);
        protected abstract string ObjectToString(T value);
    }
}
