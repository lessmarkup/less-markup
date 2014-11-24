using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Migrate
{
    class Migrator : IMigrator, IDisposable
    {
        private readonly IDbConnection _connection;
        private readonly PluralizationService _pluralizationService;

        public Migrator(IEngineConfiguration engineConfiguration)
        {
            _connection = new SqlConnection(engineConfiguration.Database);
            _connection.Open();
            _pluralizationService = PluralizationService.CreateService(new CultureInfo("en-us"));
        }

        public void ExecuteSql(string sql)
        {
            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public object ExecuteScalar(string sql)
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = sql;
                return command.ExecuteScalar();
            }
        }

        public bool CheckExists<T>() where T : IDataObject
        {
            var tableName = _pluralizationService.Pluralize(typeof (T).Name);
            return (int)ExecuteScalar(string.Format("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{0}'", tableName)) > 0;
        }

        private static string GetDataType(PropertyInfo property)
        {
            string nullable = " not null";

            var type = property.PropertyType;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>))
            {
                type = type.GenericTypeArguments[0];
                nullable = " null";
            }

            if (type == typeof (int))
            {
                return "[int]" + nullable;
            }

            if (type == typeof (DateTime))
            {
                return "[datetime]" + nullable;
            }

            if (type == typeof (long))
            {
                if (property.Name == "Id")
                {
                    return "[bigint] IDENTITY(1,1)" + nullable;
                }
                return "[bigint]" + nullable;
            }

            if (type == typeof (bool))
            {
                return "[bit]" + nullable;
            }

            if (type == typeof (double))
            {
                return "[float]" + nullable;
            }

            if (type == typeof (string))
            {
                if (property.GetCustomAttribute<RequiredAttribute>() == null)
                {
                    nullable = " null";
                }
                var max = "max";
                var maxLength = property.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLength != null)
                {
                    max = maxLength.Length.ToString(CultureInfo.InvariantCulture);
                }
                return string.Format("[nvarchar]({0}){1}", max, nullable);
            }

            if (type == typeof (byte[]))
            {
                if (property.GetCustomAttribute<RequiredAttribute>() == null)
                {
                    nullable = " null";
                }
                var max = "max";
                var maxLength = property.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLength != null)
                {
                    max = maxLength.Length.ToString(CultureInfo.InvariantCulture);
                }
                return string.Format("[varbinary]({0}){1}", max, nullable);
            }

            if (type.IsEnum)
            {
                return "[int] not null";
            }

            return null;
        }

        public void CreateTable<T>() where T : IDataObject
        {
            if (CheckExists<T>())
            {
                UpdateTable<T>();
                return;
            }

            var tableName = _pluralizationService.Pluralize(typeof(T).Name);

            var sb = new StringBuilder();
            sb.AppendFormat("CREATE TABLE [{0}] (", tableName);

            var first = true;

            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.AppendLine(",");
                }

                string dataType = GetDataType(property);
                sb.AppendFormat("[{0}] {1}", property.Name, dataType);
            }

            sb.AppendFormat(", CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED ([Id] ASC))", tableName);

            ExecuteSql(sb.ToString());
        }

        public bool CheckDependency<TD, TB>(string column = null) where TD : IDataObject where TB : IDataObject
        {
            var dependentTableName = _pluralizationService.Pluralize(typeof(TD).Name);
            var baseTableName = _pluralizationService.Pluralize(typeof(TB).Name);

            if (string.IsNullOrEmpty(column))
            {
                column = string.Format("{0}Id", typeof(TB).Name);
            }

            var text = string.Format("SELECT COUNT(*) FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_NAME = 'FK_{0}_{2}_{1}'", dependentTableName, column, baseTableName);

            return (int) ExecuteScalar(text) > 0;
        }

        public void AddDependency<TD, TB>(string column = null) where TD : IDataObject where TB : IDataObject
        {
            if (CheckDependency<TD, TB>(column))
            {
                return;
            }

            var dependentTableName = _pluralizationService.Pluralize(typeof (TD).Name);
            var baseTableName = _pluralizationService.Pluralize(typeof (TB).Name);

            if (string.IsNullOrEmpty(column))
            {
                column = string.Format("{0}Id", typeof (TB).Name);
            }

            var text = string.Format("ALTER TABLE [{0}] ADD CONSTRAINT [FK_{0}_{2}_{1}] FOREIGN KEY([{1}]) REFERENCES [{2}] ([Id])", dependentTableName, column, baseTableName);

            ExecuteSql(text);

            text = string.Format("CREATE INDEX [IX_{0}] ON [{1}] ([{0}] ASC)", column, dependentTableName);

            ExecuteSql(text);
        }

        public void DeleteDependency<TD, TB>(string column = null) where TD : IDataObject where TB : IDataObject
        {
            var dependentTableName = _pluralizationService.Pluralize(typeof (TD).Name);
            var baseTableName = _pluralizationService.Pluralize(typeof (TB).Name);

            if (string.IsNullOrEmpty(column))
            {
                column = string.Format("{0}Id", typeof (TB).Name);
            }

            var text = string.Format("DROP INDEX [IX_{0}] ON [{1}]", column, dependentTableName);

            ExecuteSql(text);

            text = string.Format("ALTER TABLE [{0}] DROP CONSTRAINT [FK_{0}_{2}_{1}]", dependentTableName, column, baseTableName);

            ExecuteSql(text);
        }

        public void UpdateTable<T>() where T : IDataObject
        {
            var tableName = _pluralizationService.Pluralize(typeof (T).Name);

            var columnsToAdd = typeof (T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.Name != "Id").ToDictionary(p => p.Name, GetDataType);
            var columnsToDrop = new List<string>();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT COLUMN_NAME, IS_NULLABLE, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '" +
                    tableName + "' AND COLUMN_NAME != 'Id'";
                command.CommandType = CommandType.Text;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var column = reader.GetString(0);

                        if (column == "Id")
                        {
                            continue;
                        }

                        string refColumnType;
                        if (!columnsToAdd.TryGetValue(column, out refColumnType))
                        {
                            columnsToDrop.Add(column);
                            continue;
                        }

                        var isNullable = reader.GetString(1) == "YES";
                        var dataType = reader.GetString(2);
                        int? maximumLength = null;
                        if (!reader.IsDBNull(3))
                        {
                            maximumLength = reader.GetInt32(3);
                        }

                        var columnType = string.Format("[{0}]", dataType);
                        if (maximumLength.HasValue)
                        {
                            if (maximumLength.Value == -1)
                            {
                                columnType += "(max)";
                            }
                            else
                            {
                                columnType += string.Format("({0})", maximumLength.Value);
                            }
                        }

                        columnType += isNullable ? " null" : " not null";

                        if (refColumnType != columnType)
                        {
                            columnsToDrop.Add(column);
                        }
                        else
                        {
                            columnsToAdd.Remove(column);
                        }
                    }
                }
            }

            foreach (var column in columnsToDrop)
            {
                ExecuteSql(string.Format("ALTER TABLE [{0}] DROP COLUMN [{1}]", tableName, column));
            }

            foreach (var columnPair in columnsToAdd)
            {
                ExecuteSql(string.Format("ALTER TABLE [{0}] ADD [{1}] {2}", tableName, columnPair.Key, columnPair.Value));
            }
        }

        public void DeleteTable<T>() where T : IDataObject
        {
            var text = string.Format("DROP TABLE [{0}]", _pluralizationService.Pluralize(typeof (T).Name));

            ExecuteSql(text);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
