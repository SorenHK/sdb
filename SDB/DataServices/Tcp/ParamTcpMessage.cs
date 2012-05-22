using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDB.DataServices.Tcp
{
    public class ParamTcpMessage : TcpMessage
    {
        private Dictionary<string, string> _params;

        public ParamTcpMessage(TcpMessage message)
            : base(message.RequestType)
        {
            SetByContent(message.Content);
        }

        public ParamTcpMessage(string requestType)
            : base(requestType)
        {
        }

        public ParamTcpMessage(TcpRequestType requestType)
            : base(requestType)
        {
        }

        private void SetByContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return;

            var currentIndex = 0;

            while (currentIndex < content.Length)
            {
                var param = ParseParameter(content, ref currentIndex);
                SetParam(param.Key, param.Value);

                if (currentIndex < content.Length && content[currentIndex] == Seperator)
                    currentIndex++;
            }
        }

        public void SetParam(string key, int? value)
        {
            SetParam(key, GetParamValueStr(value));
        }

        public void SetParam(string key, bool value)
        {
            SetParam(key, GetParamValueStr(value));
        }

        public void SetParam(string key, string value)
        {
            if (_params == null)
                _params = new Dictionary<string, string>();

            _params[key] = value;
        }

        public string GetParam(string key)
        {
            if (_params == null)
                return null;

            string value;
            return _params.TryGetValue(key, out value) ? value : null;
        }

        public int? GetParamAsNullableInt(string key)
        {
            var value = GetParam(key);

            try
            {
                if (value != null)
                    return Int32.Parse(value);
            }
            catch (FormatException) { }

            return null;
        }

        public bool HasParam(string key)
        {
            return _params != null && _params.ContainsKey(key);
        }

        protected override string GetContentAsString()
        {
            var content = new StringBuilder();

            if (_params != null)
            {
                var first = true;
                foreach (var param in _params)
                {
                    if (first)
                        first = false;
                    else
                        content.Append(Seperator);

                    content.Append(GetParamStr(param.Key, param.Value));
                }
            }

            return content.ToString();
        }
    }
}
