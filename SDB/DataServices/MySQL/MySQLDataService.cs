using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using SDB.Helpers;

namespace SDB.DataServices.MySQL
{
    public class MySQLDataService : DataServiceBase
    {
        private readonly object _lockObject;
        private readonly string _connectionString;
        private MySqlConnection _connection;
        private readonly string _itemsTable;
        private readonly string _relationsTable;

        public MySQLDataService(string connectionString = null, string tablePrefix = "sdb_")
        {
            _connectionString = connectionString;
            _lockObject = new object();

            if (_connectionString == null)
            {
                _connectionString = ConfigurationHelper.Get("mysql", "connection_string");
                if (_connectionString == null)
                    throw new ArgumentException("No connectionString was given to the MySQLDataService and the server could not find it in the applications configuration file.", "connectionString");
            }

            _itemsTable = tablePrefix + "items";
            _relationsTable = tablePrefix + "relations";

            Execute("CREATE TABLE IF NOT EXISTS " + _itemsTable + " ("
                    + "id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,"
                    + "value TEXT,"
                    + "ref_id INTEGER UNSIGNED,"
                    + "PRIMARY KEY (id)"
                    + ");");

            Execute("CREATE TABLE IF NOT EXISTS " + _relationsTable + " ("
                    + "id INTEGER UNSIGNED NOT NULL AUTO_INCREMENT,"
                    + "from_id INTEGER UNSIGNED,"
                    + "identifier VARCHAR(45) NOT NULL,"
                    + "to_id INTEGER UNSIGNED,"
                    + "relation_type INTEGER NOT NULL,"
                    + "sort_num INTEGER,"
                    + "PRIMARY KEY (id)"
                    + ");");
        }

        public override void Insert(DbItem item)
        {
            var id = ExecuteScalarAsInt(string.Format("INSERT into " + _itemsTable + "(value, ref_id) values ({0},{1}); select last_insert_id();", AsValueNullable(item.Value), AsValueNullable(item.RefId)));

            if (item.Id <= 0)
                item.Id = id;
        }

        public override void Update(DbItem item)
        {
            if (item.Id <= 0)
                Insert(item);
            else
                Execute(string.Format("UPDATE " + _itemsTable + " set value = {0}, ref_id = {1} WHERE id = {2}", AsValueNullable(item.Value), AsValueNullable(item.RefId), AsValueNullable(item.Id)));

            OnItemChanged(item.Id);
        }

        public override void Delete(DbItem item)
        {
            if (item.Id <= 0)
                return;

            Execute(string.Format("DELETE FROM " + _itemsTable + " WHERE id = {0}", AsValueNullable(item.Id)));
        }

        public override void Insert(DbRelation relation)
        {
            var id = ExecuteScalarAsInt(string.Format("INSERT into " + _relationsTable + "(from_id, identifier, to_id, relation_type, sort_num) values ({0},{1},{2},{3},{4}); select last_insert_id();",
                                                      AsValueNullable(relation.FromId), AsValueNullable(relation.Identifier),
                                                      AsValueNullable(relation.ToId), AsValueNullable(relation.RelationType), AsValueNullable(relation.SortNum)));

            if (relation.Id <= 0)
                relation.Id = id;

            OnRelationAdded(relation);
        }

        public override void Delete(DbRelation relation)
        {
            if (relation.Id <= 0)
                return;

            Execute(string.Format("DELETE FROM " + _relationsTable + " WHERE id = {0}", AsValueNullable(relation.Id)));

            OnRelationRemoved(relation);
        }

        public override ICollection<DbRelation> GetRelations(int? fromId)
        {
            return GetRelationsByQuery("SELECT * FROM " + _relationsTable + " WHERE " + IsEqual("from_id", fromId) + " ORDER BY sort_num, identifier");
        }

        public override DbRelation GetRelation(int? fromId, string identifier)
        {
            return GetRelationByQuery("SELECT * FROM " + _relationsTable + " WHERE " + IsEqual("from_id", fromId) + " AND identifier = '" + identifier + "' ORDER BY sort_num");
        }

        public override DbItem GetItem(int id)
        {
            return GetItemByQuery("SELECT * FROM " + _itemsTable + " where id = " + id);
        }

        private static string IsEqual(string key, int? value)
        {
            return key + (value != null ? " = " + value.Value : " is NULL");
        }

        private static string AsValueNullable(int? value)
        {
            return value != null ? value.Value.ToString() : "NULL";
        }

        private static string AsValueNullable(string value)
        {
            return value != null ? "'" + value + "'" : "NULL";
        }

        private static string AsValueNullable(DbRelationType? value)
        {
            return value != null ? ((int) value).ToString() : "NULL";
        }

        private ICollection<DbRelation> GetRelationsByQuery(string query)
        {
            var relations = new List<DbRelation>();

            Execute(delegate(MySqlConnection connection)
            {
                var command = connection.CreateCommand();
                command.CommandText = query;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        relations.Add(GetDbRelation(reader));
                    }
                }
            });

            return relations;
        }

        private DbRelation GetRelationByQuery(string query)
        {
            DbRelation result = null;

            Execute(delegate(MySqlConnection connection)
            {
                var command = connection.CreateCommand();
                command.CommandText = query;

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = GetDbRelation(reader);
                    }
                }
            });

            return result;
        }

        private DbItem GetItemByQuery(string query)
        {
            DbItem result = null;

            Execute(delegate(MySqlConnection connection)
            {
                var command = connection.CreateCommand();
                command.CommandText = query;

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = GetDbItem(reader);
                    }
                }
            });

            return result;
        }

        private DbItem GetDbItem(MySqlDataReader reader)
        {
            return new DbItem
                   {
                       Id = reader.GetInt32("id"),
                       Value = reader.GetNullableString("value"),
                       RefId = reader.GetNullableInt("ref_id")
                   };
        }

        private DbRelation GetDbRelation(MySqlDataReader reader)
        {
            return new DbRelation
            {
                Id = reader.GetInt32("id"),
                FromId = reader.GetNullableInt("from_id"),
                Identifier = reader.GetString("identifier"),
                ToId = reader.GetNullableInt("to_id"),
                RelationType = reader.GetIntEnum<DbRelationType>("relation_type"),
                SortNum = reader.GetNullableInt("sort_num")
            };
        }

        protected void Execute(Action<MySqlConnection> action, bool retry = true)
        {
            if (action == null)
                return;

            lock (_lockObject)
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                {
                    _connection = new MySqlConnection(_connectionString);
                    _connection.Open();
                }

                try
                {
                    action(_connection);
                }
                catch (MySqlException ex)
                {
                    if (!retry || !IsConnectionClosed(ex))
                        throw;

                    // The connection was closed (probably due to sleeping too long)
                    // Retry on a new connection
                    _connection.Dispose();

                    _connection = new MySqlConnection(_connectionString);
                    _connection.Open();
                    action(_connection);
                }
            }
        }

        protected void Execute(string nonQuery)
        {
            Execute(delegate(MySqlConnection connection)
            {
                var command = connection.CreateCommand();
                command.CommandText = nonQuery;
                command.ExecuteNonQuery();
            });
        }

        protected object ExecuteScalar(string query)
        {
            object result = null;

            Execute(delegate(MySqlConnection connection)
            {
                var command = connection.CreateCommand();
                command.CommandText = query;
                result = command.ExecuteScalar();
            });

            return result;
        }

        protected int ExecuteScalarAsInt(string query)
        {
            return Convert.ToInt32(ExecuteScalar(query));
        }

        public override void Dispose()
        {
            if (_connection != null)
                _connection.Dispose();
        }

        private static bool IsConnectionClosed(Exception exception)
        {
            if (exception == null)
                return false;

            if (!string.IsNullOrEmpty(exception.Message) && exception.Message.Contains("An established connection was aborted by the software in your host machine."))
                return true;

            return IsConnectionClosed(exception.InnerException);
        }
    }
}
