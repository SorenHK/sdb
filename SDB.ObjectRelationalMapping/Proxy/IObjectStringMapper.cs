using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDB.ObjectRelationalMapping.Proxy
{
    public interface IObjectStringMapper
    {
        object FromString(string value);
        string ToString(object value);
    }
}
