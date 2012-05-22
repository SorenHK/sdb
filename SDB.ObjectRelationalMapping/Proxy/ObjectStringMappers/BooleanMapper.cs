using System;
namespace SDB.ObjectRelationalMapping.Proxy.ObjectStringMappers
{
    class BooleanMapper : ObjectStringMapper<bool>
    {
        protected override bool ObjectFromString(string value)
        {
            return value != null && Convert.ToBoolean(value);
        }

        protected override string ObjectToString(bool value)
        {
            return value.ToString();
        }
    }
}
