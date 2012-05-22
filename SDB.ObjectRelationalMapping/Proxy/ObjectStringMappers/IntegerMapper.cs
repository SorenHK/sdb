using System;

namespace SDB.ObjectRelationalMapping.Proxy.ObjectStringMappers
{
    class IntegerMapper : ObjectStringMapper<int>
    {
        protected override int ObjectFromString(string value)
        {
            return value != null ? Convert.ToInt32(value) : 0;
        }

        protected override string ObjectToString(int value)
        {
            return value.ToString();
        }
    }
}
