using System;

namespace SDB.ObjectRelationalMapping.Proxy
{
    public class ClassPropertyLoadHandler<T> : PropertyLoadHandlerBase<T> where T : class
    {
        public ClassPropertyLoadHandler(IProxy proxy, string propertyName, ObjectMapper objectMapper)
            : base(proxy, propertyName, objectMapper)
        {
        }

        protected override T GetValue(DbItem item)
        {
            var mapper = ObjectStringMappersManager.GetMapper(PropertyType);
            if (mapper != null)
                return (T)mapper.FromString(item.Value);

            if (item.Value != null)
            {
                if (IsInvalidType)
                    throw UnsupportedType();

                int id;
                if (Int32.TryParse(item.Value, out id))
                    return ObjectMapper.GetSingle<T>(id);
            }

            return null;
        }

        protected override void SetValue(ref T value, DbItem item)
        {
            if (value == null)
            {
                item.Value = null;
                return;
            }

            var mapper = ObjectStringMappersManager.GetMapper(PropertyType);
            if (mapper != null)
            {
                item.Value = mapper.ToString(value);
                return;
            }

            if (IsInvalidType)
                throw UnsupportedType();

            value = ObjectMapper.Save(value);
            item.Value = (value as IProxy).SDBId.ToString();

            // Todo: check for attributes on the type/property to identify whether the used type should map using ToString
            //_propertyDbItem.Value = value.ToString();
        }
    }
}
