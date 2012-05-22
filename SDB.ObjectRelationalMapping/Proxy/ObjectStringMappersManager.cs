using System;
using System.Collections.Generic;
using SDB.ObjectRelationalMapping.Proxy.ObjectStringMappers;

namespace SDB.ObjectRelationalMapping.Proxy
{
    public class ObjectStringMappersManager
    {
        private static readonly Dictionary<Type, IObjectStringMapper> PropertyTypeMappers;

        static ObjectStringMappersManager()
        {
            PropertyTypeMappers = new Dictionary<Type, IObjectStringMapper>();
            Register(new StringMapper());
            Register(new IntegerMapper());
            Register(new DoubleMapper());
            Register(new BooleanMapper());
            Register(new DateTimeMapper());
        }

        public static void Register(Type type, IObjectStringMapper mapper)
        {
            PropertyTypeMappers[type] = mapper;
        }

        public static void Register<T>(ObjectStringMapper<T> mapper)
        {
            Register(typeof(T), mapper);
        }

        public static IObjectStringMapper GetMapper(Type type)
        {
            IObjectStringMapper result;
            PropertyTypeMappers.TryGetValue(type, out result);
            return result;
        }
    }
}
