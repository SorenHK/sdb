using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDB.DataServices.Tcp
{
    public class ObjectTcpMessage<T> : TcpMessage where T : class
    {
        private List<T> _items = new List<T>();

        public ICollection<T> Items { get { return _items; } }
        public T Item { get { return _items != null ? _items.FirstOrDefault() : default(T); } }

        public ObjectTcpMessage(TcpRequestType requestType)
            : base(requestType)
        {
        }

        public ObjectTcpMessage(string requestType)
            : base(requestType)
        {
        }

        public ObjectTcpMessage(TcpMessage message)
            : this(message.RequestType)
        {
            SetByContent(message.Content);
        }

        private void SetByContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return;

            if (!content.StartsWith("[") || !content.EndsWith("]"))
                throw new InvalidOperationException("Unrecognized format of TCP message content: " + content);

            var itemConstructor = typeof(T).GetConstructor(new Type[0]);
            if (itemConstructor == null)
                throw new InvalidOperationException("Type " + typeof(T) + " does not have a constructor without arguments");

            var properties = typeof(T).GetProperties();

            var currentIndex = 1;

            while (content[currentIndex] != ']')
            {
                if (content[currentIndex] != '<')
                    throw new InvalidOperationException("Badly formatted item list: " + content);

                currentIndex++;

                var item = (T)itemConstructor.Invoke(null);

                while (content[currentIndex] != '>')
                {
                    var param = ParseParameter(content, ref currentIndex);

                    foreach (var prop in properties)
                    {
                        if (prop.Name.Equals(param.Key))
                        {
                            object value;

                            if (prop.PropertyType == typeof(int))
                            {
                                value = Convert.ToInt32(param.Value);
                            }
                            else if (prop.PropertyType == typeof(int?))
                            {
                                value = param.Value != null ? Convert.ToInt32(param.Value) : new int?();
                            }
                            else if (prop.PropertyType == typeof(string))
                            {
                                value = param.Value;
                            }
                            else if (prop.PropertyType.IsEnum)
                            {
                                try
                                {
                                    value = Enum.ToObject(prop.PropertyType, Int32.Parse(param.Value));
                                }
                                catch(FormatException)
                                {
                                    value = Enum.Parse(prop.PropertyType, param.Value);
                                }
                            }
                            else
                                throw new NotImplementedException("The property type " + prop.PropertyType + " is not supported in ObjectTcpMessage");

                            prop.SetValue(item, value, null);
                            break;
                        }
                    }

                    if (currentIndex < content.Length && content[currentIndex] == Seperator)
                        currentIndex++;
                }

                Add(item);

                currentIndex++; // Skip the '>'

                if (currentIndex < content.Length && content[currentIndex] == Seperator)
                    currentIndex++;
            }
        }

        public void Add(T item)
        {
            if (item == null)
                return;

            _items.Add(item);
        }

        public void Add(IEnumerable<T> items)
        {
            if (items == null)
                return;

            _items.AddRange(items);
        }

        protected override string GetContentAsString()
        {
            var content = new StringBuilder();

            content.Append('[');

            if (_items.Any())
            {
                var properties = typeof(T).GetProperties();

                var firstItem = true;
                foreach (var item in _items)
                {
                    if (firstItem)
                        firstItem = false;
                    else
                        content.Append(Seperator);

                    content.Append("<");

                    var firstProp = true;
                    foreach (var prop in properties)
                    {
                        if (firstProp)
                            firstProp = false;
                        else
                            content.Append(Seperator);

                        string value;

                        if (prop.PropertyType == typeof(int))
                        {
                            value = GetParamValueStr((int)prop.GetValue(item, null));
                        }
                        else if (prop.PropertyType == typeof(int?))
                        {
                            value = GetParamValueStr((int?)prop.GetValue(item, null));
                        }
                        else if (prop.PropertyType == typeof(string))
                        {
                            value = (string)prop.GetValue(item, null);
                        }
                        else if (prop.PropertyType.IsEnum)
                        {
                            value = GetParamValueStr((int)prop.GetValue(item, null));
                        }
                        else
                            throw new NotImplementedException("The property type " + prop.PropertyType + " is not supported in ObjectTcpMessage");

                        content.Append(GetParamStr(prop.Name, value));
                    }

                    content.Append('>');
                }
            }

            content.Append(']');

            return content.ToString();
        }
    }
}
