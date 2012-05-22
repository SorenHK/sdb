using System;

namespace SDB.ObjectRelationalMapping.Proxy.ObjectStringMappers
{
    class DateTimeMapper : ObjectStringMapper<DateTime>
    {
        private const string Format = "yyyy-MM-dd HH:mm:ss";

        protected override DateTime ObjectFromString(string value)
        {
            return value != null ? DateTime.ParseExact(value, Format, null) : new DateTime();
        }

        protected override string ObjectToString(DateTime value)
        {
            return value.ToString(Format);
        }
    }
}
