using System;

namespace SDB.ObjectRelationalMapping.Proxy
{
    public class StructPropertyLoadHandler<T> : PropertyLoadHandlerBase<T> where T : struct
    {
        public StructPropertyLoadHandler(IProxy proxy, string propertyName, ObjectMapper objectMapper)
            : base(proxy, propertyName, objectMapper)
        {
        }

        protected override T GetValue(DbItem item)
        {
            if (PropertyType.IsEnum)
                return (T)Enum.ToObject(PropertyType, item.Value != null ? Convert.ToInt32(item.Value) : 0);

            var mapper = ObjectStringMappersManager.GetMapper(PropertyType);
            if (mapper != null)
                return (T)mapper.FromString(item.Value);

            if (item.Value == null)
                return default(T);

            throw UnsupportedType();
        }

        protected override void SetValue(ref T value, DbItem item)
        {
            if (PropertyType.IsEnum)
            {
                item.Value = (Convert.ToInt32(value)).ToString(); // http://stackoverflow.com/questions/908543/how-to-convert-from-system-enum-to-base-integer
                return;
            }

            var mapper = ObjectStringMappersManager.GetMapper(PropertyType);
            if (mapper != null)
            {
                item.Value = mapper.ToString(value);
                return;
            }

            throw UnsupportedType();

            // Todo: check for attributes on the type/property to identify whether the used type should map using ToString
            //_propertyDbItem.Value = value.ToString();
        }
    }
}
