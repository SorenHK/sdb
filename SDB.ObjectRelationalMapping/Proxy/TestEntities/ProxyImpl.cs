using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SDB.ObjectRelationalMapping.Proxy.TestEntities
{
    class ProxyImpl : TestClass, IProxy, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int SDBId { get; set; }

        private ClassPropertyLoadHandler<string> _stringPropertyHandler;
        public string StringProperty
        {
            get { return _stringPropertyHandler.Value; }
            set { _stringPropertyHandler.Value = value; }
        }

        private StructPropertyLoadHandler<int> _integerPropertyHandler;
        public int IntegerProperty
        {
            get { return _integerPropertyHandler.Value; }
            set { _integerPropertyHandler.Value = value; }
        }

        private StructPropertyLoadHandler<TestEnumImpl> _enumPropertyHandler;
        public TestEnumImpl EnumProperty
        {
            get { return _enumPropertyHandler.Value; }
            set { _enumPropertyHandler.Value = value; }
        }

        private StructPropertyLoadHandler<DateTime> _dateTimePropertyHandler;
        public DateTime DateTimeProperty
        {
            get { return _dateTimePropertyHandler.Value; }
            set { _dateTimePropertyHandler.Value = value; }
        }

        private CollectionPropertyLoadHandler<ICollection<TestClass>, TestClass>  _collectionPropertyHandler;
        public ICollection<TestClass> CollectionProperty
        {
            get { return _collectionPropertyHandler.Value; }
            set { _collectionPropertyHandler.Value = value; }
        }

        public ProxyImpl(ObjectMapper objectMapper)
        {
            _stringPropertyHandler = new ClassPropertyLoadHandler<string>(this, "StringProperty", objectMapper);
            _integerPropertyHandler = new StructPropertyLoadHandler<int>(this, "IntegerProperty", objectMapper);
            _enumPropertyHandler = new StructPropertyLoadHandler<TestEnumImpl>(this, "EnumProperty", objectMapper);
            _dateTimePropertyHandler = new StructPropertyLoadHandler<DateTime>(this, "DateTimeProperty", objectMapper);
            _collectionPropertyHandler = new CollectionPropertyLoadHandler<ICollection<TestClass>, TestClass>(this, "CollectionProperty", objectMapper);
        }

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool Equals(object obj)
        {
            if (obj is IProxy)
                return (obj as IProxy).SDBId == SDBId;

            return base.Equals(obj);
        }
    }

    enum TestEnumImpl
    {
        Test0 = 0,
        Test1 = 1
    }
}
