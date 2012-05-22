using System;
using System.Globalization;

namespace SDB.ObjectRelationalMapping.Proxy.ObjectStringMappers
{
    class DoubleMapper : ObjectStringMapper<double>
    {
        protected override double ObjectFromString(string value)
        {
            return value != null ? Convert.ToDouble(value) : 0;
        }

        protected override string ObjectToString(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
