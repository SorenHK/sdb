using System;

namespace SDB.ObjectRelationalMapping.Proxy
{
    public abstract class PropertyLoadHandlerBase<T>
    {
        private DbItem _item;

        protected string PropertyName { get; private set; }
        protected IProxy Proxy { get; private set; }
        protected ObjectMapper ObjectMapper { get; private set; }

        protected PropertyLoadHandlerBase(IProxy proxy, string propertyName, ObjectMapper objectMapper)
        {
            ObjectMapper = objectMapper;
            Proxy = proxy;
            PropertyName = propertyName;
            ObjectMapper.DataService.ItemChanged += dataService_ItemChanged;
        }

        void dataService_ItemChanged(int id)
        {
            if (_item != null && id == _item.Id)
            {
                IsLoaded = false;
                Load();
                Proxy.OnPropertyChanged(PropertyName);
            }
        }

        public bool IsLoaded { get; set; }
        protected static Type PropertyType { get { return typeof(T); } }
        protected static bool IsInvalidType { get { return PropertyType.IsValueType || PropertyType.IsInterface || PropertyType.IsGenericType; } }

        private T _value;
        public T Value
        {
            get
            {
                Load();
                return _value;
            }
            set
            {
                Load();
                if (PropertyType.IsValueType ? _value.Equals(value) : (value == null && _value == null) || (_value != null && _value.Equals(value)))
                    return;

                var itemValueBefore = _item.Value;

                SetValue(ref value, _item);

                _value = value;

                if (itemValueBefore == _item.Value)
                    return; // No changes to the DbItem

                ObjectMapper.DataService.Update(_item);

                Proxy.OnPropertyChanged(PropertyName);
            }
        }

        private void Load()
        {
            if (IsLoaded)
                return;

            lock (this)
            {
                if (IsLoaded)
                    return;

                if (_item == null)
                    _item = ObjectMapper.DataService.GetOrCreateItem(Proxy.SDBId, PropertyName);
                else
                    _item = ObjectMapper.DataService.GetItem(_item.Id); // Reload

                _value = GetValue(_item);

                IsLoaded = true;
            }
        }

        protected Exception UnsupportedType()
        {
            throw new InvalidOperationException("Property '" + PropertyName + "' of " + Proxy.GetType().Name + " has an unsupported type: " + PropertyType.Name);
        }

        protected abstract T GetValue(DbItem item);
        protected abstract void SetValue(ref T value, DbItem item);
    }
}
