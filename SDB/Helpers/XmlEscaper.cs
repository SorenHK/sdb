namespace SDB.Helpers
{
    class XmlEscaper
    {
        public static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            var result = s;
            result = result.Replace("&", "&amp;");
            result = result.Replace("'", "&apos;");
            result = result.Replace("\"", "&quot;");
            result = result.Replace(">", "&gt;");
            result = result.Replace("<", "&lt;");

            return result;
        }

        public static string Unescape(string s)
        {
            if (string.IsNullOrEmpty(s)) 
                return s;

            var returnString = s;
            returnString = returnString.Replace("&apos;", "'");
            returnString = returnString.Replace("&quot;", "\"");
            returnString = returnString.Replace("&gt;", ">");
            returnString = returnString.Replace("&lt;", "<");
            returnString = returnString.Replace("&amp;", "&");

            return returnString;
        }
    }
}
