namespace SDB.ObjectRelationalMapping.Proxy.ObjectStringMappers
{
    class StringMapper : ObjectStringMapper<string>
    {
        protected override string ObjectFromString(string value)
        {
            return value;
        }

        protected override string ObjectToString(string value)
        {
            return value;
        }
    }
}
