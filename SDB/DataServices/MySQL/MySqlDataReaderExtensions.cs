using System;
using MySql.Data.MySqlClient;

namespace SDB.DataServices.MySQL
{
    static class MySqlDataReaderExtensions
    {
        public static string GetNullableString(this MySqlDataReader reader, string column)
        {
            return GetNullableString(reader, reader.GetOrdinal(column));
        }

        public static string GetNullableString(this MySqlDataReader reader, int colIndex)
        {
            return !reader.IsDBNull(colIndex) ? reader.GetString(colIndex) : null;
        }

        public static int? GetNullableInt(this MySqlDataReader reader, string column)
        {
            return GetNullableInt(reader, reader.GetOrdinal(column));
        }

        public static int? GetNullableInt(this MySqlDataReader reader, int colIndex)
        {
            return !reader.IsDBNull(colIndex) ? reader.GetInt32(colIndex) : new int?();
        }

        public static T GetIntEnum<T>(this MySqlDataReader reader, string column)
        {
            var val = reader.GetInt32(column);
            return (T)Enum.ToObject(typeof(T), val);
        }

        public static T GetIntEnum<T>(this MySqlDataReader reader, int colIndex)
        {
            var val = reader.GetInt32(colIndex);
            return (T)Enum.ToObject(typeof(T), val);
        }
    }
}
