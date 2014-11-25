using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.System;

namespace LessMarkup.DataFramework.Light
{
    public class LightDomainModel : ILightDomainModel
    {
        private IDbConnection _connnection;
        private TransactionScope _transactionScope;
        private readonly IEngineConfiguration _engineConfiguration;
        private static readonly Dictionary<Type, TableMetadata> _tableMetadatas = new Dictionary<Type, TableMetadata>();
        private static readonly object _tableMetadataLock = new object();
        private static readonly Dictionary<Type, int> _collectionTypeToId = new Dictionary<Type, int>();
        private static readonly Dictionary<int, Type> _collectionIdToType = new Dictionary<int, Type>(); 
        private static int _collectionIdCounter = 1;

        public LightDomainModel(IEngineConfiguration engineConfiguration)
        {
            _engineConfiguration = engineConfiguration;
        }

        public static void RegisterDataType(Type type)
        {
            _collectionTypeToId[type] = _collectionIdCounter;
            _collectionIdToType[_collectionIdCounter] = type;
            _collectionIdCounter++;
            _tableMetadatas[type] = new TableMetadata(type);
        }

        public static int GetCollectionId<T>() where T : IDataObject
        {
            return _collectionTypeToId[typeof (T)];
        }

        public static List<Type> GetDataTypes()
        {
            return _collectionTypeToId.Keys.ToList();
        } 

        public static int GetCollectionId(Type collectionType)
        {
            return _collectionTypeToId[collectionType];
        }

        public static Type GetCollectionType(int collectionId)
        {
            return _collectionIdToType[collectionId];
        }

        private IDbConnection CreateConnection()
        {
            var connectionString = _engineConfiguration.Database;
            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        internal IDbConnection Connection
        {
            get { return _connnection ?? (_connnection = CreateConnection()); }
        }

        internal static TableMetadata GetMetadata<T>() where T : IDataObject
        {
            TableMetadata ret;
            if (_tableMetadatas.TryGetValue(typeof (T), out ret))
            {
                return ret;
            }

            lock (_tableMetadataLock)
            {
                ret = new TableMetadata(typeof (T));
                _tableMetadatas[typeof (T)] = ret;
            }

            return ret;
        }

        public ILightQueryBuilder Query()
        {
            return new QueryBuilder(this);
        }

        internal void CreateTransaction()
        {
            if (_transactionScope == null)
            {
                _transactionScope = new TransactionScope();
            }
        }

        public void CompleteTransaction()
        {
            if (_transactionScope == null)
            {
                throw new InvalidOperationException("Cannot complete empty transaction");
            }

            _transactionScope.Complete();
        }

        public void Update<T>(T dataObject) where T : IDataObject
        {
            var metadata = GetMetadata<T>();

            using (var command = Connection.CreateCommand())
            {
                var text = string.Format("UPDATE [{0}] SET ", metadata.Name);

                var first = true;
                var index = 0;

                foreach (var column in metadata.Columns)
                {
                    if (column.Key == "Id")
                    {
                        continue;
                    }

                    if (!first)
                    {
                        text += ", ";
                    }
                    first = false;

                    var name = string.Format("@_p{0}", index++);

                    text += string.Format("[{0}] = {1}", column.Key, name);

                    var value = column.Value.GetValue(dataObject);

                    var parameter = command.CreateParameter();
                    parameter.ParameterName = name;
                    parameter.Value = value ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }

                text += string.Format(" WHERE Id = {0}", dataObject.Id);

                command.CommandText = text;
                command.CommandType = CommandType.Text;
                ExecuteNonQuery(command);
            }
        }

        public void Create<T>(T dataObject) where T : IDataObject
        {
            var metadata = GetMetadata<T>();

            using (var command = Connection.CreateCommand())
            {
                var values = new List<string>();
                var names = new List<string>();
                var index = 0;

                foreach (var column in metadata.Columns)
                {
                    if (column.Key == "Id")
                    {
                        continue;
                    }

                    var name = string.Format("@_p{0}", index++);

                    var value = column.Value.GetValue(dataObject);

                    if (value == null)
                    {
                        continue;
                    }

                    names.Add(column.Key);
                    values.Add(name);

                    var parameter = command.CreateParameter();
                    parameter.ParameterName = name;
                    parameter.Value = value;
                    command.Parameters.Add(parameter);
                }

                var text = string.Format("INSERT INTO [{0}] ({1}) OUTPUT INSERTED.ID VALUES ({2})", metadata.Name, string.Join(",", names), string.Join(",", values));
                command.CommandText = text;
                command.CommandType = CommandType.Text;

                dataObject.Id = ExecuteScalar<long>(command);
            }
        }

        public void Delete<T>(long id) where T : IDataObject
        {
            var metadata = GetMetadata<T>();

            using (var command = Connection.CreateCommand())
            {
                command.CommandText = string.Format("DELETE FROM [{0}] WHERE Id={1}", metadata.Name, id);
                command.CommandType = CommandType.Text;
                ExecuteNonQuery(command);
            }
        }

        public void Dispose()
        {
            if (_transactionScope != null)
            {
                _transactionScope.Dispose();
                _transactionScope = null;
            }

            if (_connnection != null)
            {
                _connnection.Dispose();
                _connnection = null;
            }
        }

        private T ExecuteScalar<T>(IDbCommand command)
        {
            try
            {
                return (T) command.ExecuteScalar();
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
                throw;
            }
        }

        private void ExecuteNonQuery(IDbCommand command)
        {
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
                throw;
            }
        }
    }
}
