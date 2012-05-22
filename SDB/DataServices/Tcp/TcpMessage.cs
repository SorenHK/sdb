using System;
using System.Collections.Generic;
using SDB.Helpers;

namespace SDB.DataServices.Tcp
{
    public class TcpMessage
    {
        private const char ContentDelimiter = ':';
        protected const char Seperator = ',';
        protected const string NullValue = "null";
        protected const char StringQuoter = '"';

        private string _content;
        public string Content { get { return GetContentAsString(); } set { _content = value; } }
        public string RequestType { get; private set; }
        public TcpRequestType KnownRequestType { get { return RequestFromStr(RequestType); } set { RequestType = RequestToStr(value); } }

        protected TcpMessage()
        {
        }

        public TcpMessage(string requestType)
        {
            RequestType = requestType;
        }

        public TcpMessage(TcpRequestType requestType)
        {
            KnownRequestType = requestType;
        }

        public static TcpMessage FromRaw(string rawContent)
        {
            var message = new TcpMessage();
            message.SetRaw(rawContent);
            return message;
        }

        protected void SetRaw(string rawContent)
        {
            if (string.IsNullOrEmpty(rawContent))
                return;

            var delimitIndex = rawContent.IndexOf(ContentDelimiter);
            if (delimitIndex <= 0)
                throw new InvalidOperationException("Unsupported format of TcpRequest: " + rawContent);

            RequestType = rawContent.Substring(0, delimitIndex);

            string content = null;
            if (delimitIndex < rawContent.Length - 1)
                content = rawContent.Substring(delimitIndex + 1, rawContent.Length - delimitIndex - 1);

            Content = content;
        }

        protected static KeyValuePair<string, string> ParseParameter(string content, ref int currentIndex)
        {
            var endIndex = content.IndexOf('=', currentIndex);
            if (endIndex < 0)
                throw new InvalidOperationException("Unrecognized format of TCP message content (badly formatted parameter for item): " + content);

            var key = content.Substring(currentIndex, endIndex - currentIndex);
            currentIndex = endIndex + 1;

            var quoted = false;

            if (content[currentIndex] == StringQuoter)
            {
                endIndex = content.IndexOf(StringQuoter, ++currentIndex);
                quoted = true;
            }
            else
            {
                var andIndex = content.IndexOf(Seperator, currentIndex);
                var objectEndIndex = content.IndexOf('>', currentIndex) - 1;

                // Take the nearest match
                endIndex = Math.Min(andIndex, objectEndIndex);
                if (endIndex < 0)
                    endIndex = Math.Max(andIndex, objectEndIndex);
            }

            if (endIndex < 0)
                throw new InvalidOperationException("Unrecognized format of TCP message content (bad quotation of item parameter value): " + content);

            string value;
            if (endIndex > currentIndex)
                value = XmlEscaper.Unescape(content.Substring(currentIndex, endIndex - currentIndex));
            else if (quoted)
                value = string.Empty;
            else
                value = null;

            currentIndex = endIndex;

            if (quoted || value == null)
                currentIndex++;

            return new KeyValuePair<string, string>(key, value);
        }

        public override string ToString()
        {
            return RequestType + ContentDelimiter + GetContentAsString();
        }

        protected virtual string GetContentAsString()
        {
            return _content ?? string.Empty;
        }

        protected static string GetParamValueStr(bool value)
        {
            return value ? "true" : "false";
        }

        protected static string GetParamValueStr(int? value)
        {
            return value != null ? value.ToString() : null;
        }

        protected static string GetParamStr(string key, string value)
        {
            if (value == null)
                return key + '=';
            return key + '=' + StringQuoter + XmlEscaper.Escape(value) + StringQuoter;
        }

        public bool HasType(string requestType)
        {
            if (string.IsNullOrEmpty(requestType))
                return string.IsNullOrEmpty(RequestType);

            return requestType.Equals(RequestType, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool HasType(TcpRequestType requestType)
        {
            return KnownRequestType == requestType;
        }

        private static string RequestToStr(TcpRequestType type)
        {
            switch (type)
            {
                case TcpRequestType.MultiRelationQuery:
                    return "mrq";
                case TcpRequestType.UniqueRelationQuery:
                    return "urq";
                case TcpRequestType.InsertRelation:
                    return "ir";
                case TcpRequestType.DeleteRelation:
                    return "dr";

                case TcpRequestType.UniqueItemQuery:
                    return "uiq";
                case TcpRequestType.InsertItem:
                    return "ii";
                case TcpRequestType.UpdateItem:
                    return "ui";
                case TcpRequestType.DeleteItem:
                    return "di";

                case TcpRequestType.List:
                    return "lst";
                case TcpRequestType.Ok:
                    return "ok";
                case TcpRequestType.Error:
                    return "err";
            }
            return "unk";
        }

        private static TcpRequestType RequestFromStr(string type)
        {
            if (!string.IsNullOrEmpty(type))
            {
                switch (type.ToLower())
                {
                    case "mrq":
                        return TcpRequestType.MultiRelationQuery;
                    case "urq":
                        return TcpRequestType.UniqueRelationQuery;
                    case "ir":
                        return TcpRequestType.InsertRelation;
                    case "ur":
                        return TcpRequestType.UpdateRelation;
                    case "dr":
                        return TcpRequestType.DeleteRelation;

                    case "uiq":
                        return TcpRequestType.UniqueItemQuery;
                    case "ii":
                        return TcpRequestType.InsertItem;
                    case "ui":
                        return TcpRequestType.UpdateItem;
                    case "di":
                        return TcpRequestType.DeleteItem;

                    case "lst":
                        return TcpRequestType.List;
                    case "ok":
                        return TcpRequestType.Ok;
                    case "err":
                        return TcpRequestType.Error;
                }
            }
            return TcpRequestType.Unknown;
        }

        public static TcpMessage Error(string error)
        {
            var response = new ParamTcpMessage(TcpRequestType.Error);
            response.SetParam("error", error);
            return response;
        }
    }

    public enum TcpRequestType
    {
        MultiRelationQuery,
        UniqueRelationQuery,
        InsertRelation,
        UpdateRelation,
        DeleteRelation,

        UniqueItemQuery,
        InsertItem,
        UpdateItem,
        DeleteItem,

        List,
        Ok,
        Error,
        Unknown
    }
}
