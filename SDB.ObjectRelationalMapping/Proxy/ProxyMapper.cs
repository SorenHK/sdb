using System;
using System.Collections.Generic;

namespace SDB.ObjectRelationalMapping.Proxy
{
    static class ProxyMapper
    {
        private static ProxyFactory _proxyFactory;
        private static Dictionary<Type, Type> _proxyTypeDic;

        static ProxyMapper()
        {
            _proxyFactory = new ProxyFactory();
            _proxyTypeDic = new Dictionary<Type, Type>();
        }

        public static Type GetProxyType(Type type)
        {
            Type proxyType;
            _proxyTypeDic.TryGetValue(type, out proxyType);
            if (proxyType == null)
            {
                proxyType = _proxyFactory.CreateType(type.Name + "Proxy", type);
                _proxyTypeDic[type] = proxyType;
            }
            return proxyType;
        }

        public static bool IsProxy(object obj)
        {
            return obj is IProxy;
        }

        public static T Save<T>(T obj, int id, ObjectMapper objectMapper) 
            where T : class
        {
            if (obj == null)
                return null;

            var type = obj.GetType();

            var proxyType = GetProxyType(type);

            var proxy = New<T>(id, objectMapper, proxyType);

            foreach (var prop in type.GetProperties())
            {
                proxyType.GetProperty(prop.Name).SetValue(proxy, prop.GetValue(obj, null), null);
            }

            return proxy;
        }

        public static IProxy New(Type type, int id, ObjectMapper objectMapper)
        {
            var proxyType = GetProxyType(type);

            return (IProxy)New(id, objectMapper, proxyType);
        }

        public static T New<T>(int id, ObjectMapper objectMapper)
        {
            var proxyType = GetProxyType(typeof(T));

            return New<T>(id, objectMapper, proxyType);
        }

        public static T New<T>(int id, ObjectMapper objectMapper, Type proxyType)
        {
            return (T)New(id, objectMapper, proxyType);
        }

        public static object New(int id, ObjectMapper objectMapper, Type proxyType)
        {
            var instance = Activator.CreateInstance(proxyType, new object[] { objectMapper });
            var proxy = (IProxy)instance;
            proxy.SDBId = id;
            return instance;
        }
    }
}
