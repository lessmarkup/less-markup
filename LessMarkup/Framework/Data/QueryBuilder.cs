using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using LessMarkup.Interfaces.Data;

#if DEBUG
using System.Diagnostics;
#endif

namespace LessMarkup.Framework.Data
{
    public class QueryBuilder : IQueryBuilder
    {
        private readonly DomainModel _domainModel;
        private IDbCommand _command;
        private string _commandText;
        private int _parameterIndex;
        private string _select = "*";
        private string _where = "";
        private string _orderBy = "";
        private string _limit = "";

        internal QueryBuilder(DomainModel domainModel)
        {
            _domainModel = domainModel;
        }

        private IDataReader ExecuteReader()
        {
#if DEBUG
            try
            {
                return _command.ExecuteReader();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                throw;
            }
#else
            return _command.ExecuteReader();
#endif
        }

        private IDbCommand GetCommand()
        {
            if (_command != null)
            {
                return _command;
            }

            _command = _domainModel.Connection.CreateCommand();
            _command.CommandType = CommandType.Text;
            return _command;
        }

        public IQueryBuilder From<T>(string name = null) where T : IDataObject
        {
            var metadata = DomainModel.GetMetadata<T>();

            _commandText += string.Format(" FROM [{0}] {1}", metadata.Name, name ?? "");

            return this;
        }

        public IQueryBuilder Join<T>(string name, string @on) where T : IDataObject
        {
            var metadata = DomainModel.GetMetadata<T>();

            _commandText += string.Format(" JOIN [{0}] {1} ON {2}", metadata.Name, name, @on);

            return this;
        }

        public IQueryBuilder LeftJoin<T>(string name, string @on) where T : IDataObject
        {
            var metadata = DomainModel.GetMetadata<T>();

            _commandText += string.Format(" LEFT JOIN [{0}] {1} ON {2}", metadata.Name, name, @on);

            return this;
        }

        public IQueryBuilder RightJoin<T>(string name, string @on) where T : IDataObject
        {
            var metadata = DomainModel.GetMetadata<T>();

            _commandText += string.Format(" RIGHT JOIN [{0}] {1} ON {2}", metadata.Name, name, @on);

            return this;
        }

        public IQueryBuilder GroupBy(string name)
        {
            _commandText += string.Format(" GROUP BY {0} ", name);
            return this;
        }

        private DbType GetDbType(Type parameterType)
        {
            if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof (Nullable<>))
            {
                parameterType = parameterType.GetGenericArguments()[0];
            }

            if (parameterType == typeof (string))
            {
                return DbType.String;
            }
            if (parameterType == typeof (int))
            {
                return DbType.Int32;
            }
            if (parameterType == typeof (long))
            {
                return DbType.Int64;
            }
            if (parameterType == typeof (DateTime))
            {
                return DbType.DateTime2;
            }
            if (parameterType == typeof (double))
            {
                return DbType.Double;
            }
            if (parameterType == typeof (bool))
            {
                return DbType.Boolean;
            }
            if (parameterType == typeof (byte[]))
            {
                return (DbType) SqlDbType.VarBinary;
            }
            if (parameterType.IsEnum)
            {
                return DbType.Int32;
            }
            throw new ArgumentOutOfRangeException("parameterType");
        }

        public IQueryBuilder Where(string filter, params object[] args)
        {
            filter = ProcessStringWithParameters(filter, args);

            if (_where.Length > 0)
            {
                _where += " AND " + filter;
            }
            else
            {
                _where = filter;
            }

            return this;
        }

        public IQueryBuilder WhereIds(IEnumerable<long> ids)
        {
            return Where(string.Format("[Id] IN ({0})", string.Join(",", ids)));
        }

        public IQueryBuilder OrderBy(string column)
        {
            if (_orderBy.Length > 0)
            {
                _orderBy += string.Format(", {0}", column);
            }
            else
            {
                _orderBy = string.Format("ORDER BY {0}", column);
            }
            return this;
        }

        public IQueryBuilder OrderByDescending(string column)
        {
            if (_orderBy.Length > 0)
            {
                _orderBy += string.Format(", {0} DESC", column);
            }
            else
            {
                _orderBy = string.Format("ORDER BY {0} DESC", column);
            }
            return this;
        }

        public IQueryBuilder Limit(int @from, int count)
        {
            _limit = string.Format("OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", @from, count);
            return this;
        }

        public T Find<T>(long id) where T : class, IDataObject
        {
            if (string.IsNullOrEmpty(_commandText))
            {
                From<T>();
            }

            return Where("[Id] = $", id).First<T>();
        }

        public T FindOrDefault<T>(long id) where T : class, IDataObject
        {
            if (string.IsNullOrEmpty(_commandText))
            {
                From<T>();
            }

            return Where("Id = $", id).FirstOrDefault<T>();
        }

        private IDbDataParameter CreateParameter(object value)
        {
            var command = GetCommand();
            var name = string.Format("@_p{0}", _parameterIndex++);
            var parameter = command.CreateParameter();
            parameter.DbType = value != null ? GetDbType(value.GetType()) : DbType.Int32;
            if (parameter.DbType == DbType.String || parameter.DbType == (DbType)SqlDbType.VarBinary)
            {
                parameter.Size = -1;
            }
            parameter.Value = value ?? DBNull.Value;
            parameter.ParameterName = name;
            command.Parameters.Add(parameter);

            return parameter;
        }

        private string ProcessStringWithParameters(string sql, object[] args)
        {
            if (args.Length == 0)
            {
                return sql;
            }

            var names = new List<string>();

            foreach (var arg in args)
            {
                names.Add(CreateParameter(arg).ParameterName);
            }

            var argIndex = 0;

            for (;;)
            {
                int pos = sql.IndexOf('$');
                if (pos < 0)
                {
                    break;
                }

                string name;
                int len = 1;

                bool isTableName = pos + 1 < sql.Length && sql[pos + 1] == '-';
                if (isTableName)
                {
                    var pos1 = pos+2;
                    for(; pos1 < sql.Length && char.IsLetter(sql[pos1]); pos1++)
                    { }
                    var tableName = sql.Substring(pos + 2, pos1 - pos - 2);
                    tableName = DomainModel.GetMetadata(tableName).Name;
                    sql = sql.Remove(pos, pos1 - pos).Insert(pos, "[" + tableName + "]");
                    continue;
                }

                if (pos + 1 < sql.Length && char.IsDigit(sql[pos + 1]))
                {
                    var digitLen = 1;
                    for (; pos + digitLen < sql.Length && char.IsDigit(sql[pos]); digitLen++)
                    { }
                    len = digitLen + 1;
                    name = names[int.Parse(sql.Substring(pos + 1, digitLen))];
                }
                else
                {
                    name = names[argIndex++];
                }

                sql = sql.Remove(pos, len).Insert(pos, name);
            }

            return sql;
        }

        private List<T> ExecuteWithLimit<T>(string sql, int? limit, params object[] args) where T : class
        {
            GetCommand().CommandText = ProcessStringWithParameters(sql, args);

            using (var reader = ExecuteReader())
            {
                var properties = new Dictionary<PropertyInfo, int>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var fieldName = reader.GetName(i);
                    var property = typeof (T).GetProperty(fieldName);
                    if (property != null)
                    {
                        properties[property] = i;
                    }
                }

                var result = new List<T>();

                while (reader.Read())
                {
                    var obj = Interfaces.DependencyResolver.TryResolve<T>() ?? Activator.CreateInstance<T>();

                    foreach (var property in properties)
                    {
                        if (reader.IsDBNull(property.Value))
                        {
                            continue;
                        }
                        switch (GetDbType(property.Key.PropertyType))
                        {
                            case DbType.String:
                                property.Key.SetValue(obj, reader.GetString(property.Value));
                                break;
                            case DbType.Int32:
                                property.Key.SetValue(obj, reader.GetInt32(property.Value));
                                break;
                            case DbType.Int64:
                                property.Key.SetValue(obj, reader.GetInt64(property.Value));
                                break;
                            case DbType.DateTime2:
                                property.Key.SetValue(obj, reader.GetDateTime(property.Value));
                                break;
                            case DbType.Double:
                                property.Key.SetValue(obj, reader.GetDouble(property.Value));
                                break;
                            case DbType.Boolean:
                                property.Key.SetValue(obj, reader.GetBoolean(property.Value));
                                break;
                            case (DbType) SqlDbType.VarBinary:
                                property.Key.SetValue(obj, reader.GetValue(property.Value));
                                break;
                        }
                    }

                    result.Add(obj);

                    if (limit.HasValue && result.Count >= limit.Value)
                    {
                        break;
                    }
                }

                _command.Dispose();
                _command = null;

                return result;
            }
        }

        public List<T> Execute<T>(string sql, params object[] args) where T : class
        {
            return ExecuteWithLimit<T>(sql, null, args);
        }

        public void ExecuteNonQuery(string sql, params object[] args)
        {
            GetCommand().CommandText = ProcessStringWithParameters(sql, args);
            GetCommand().ExecuteNonQuery();
        }

        public T ExecuteScalar<T>(string sql, params object[] args)
        {
            GetCommand().CommandText = ProcessStringWithParameters(sql, args);
            return (T) GetCommand().ExecuteScalar();
        }

        private string GetSql()
        {
            var ret = "SELECT " + _select + " " + _commandText;

            if (_where.Length > 0)
            {
                ret += " WHERE " + _where;
            }

            if (_orderBy.Length > 0)
            {
                ret += " " + _orderBy;
            }

            if (_limit.Length > 0)
            {
                ret += " " + _limit;
            }

            return ret;
        }

        public List<T> ToList<T>(string selectText = null) where T : class
        {
            if (!string.IsNullOrEmpty(selectText))
            {
                _select = selectText;
            }

            return Execute<T>(GetSql());
        }

        public int Count()
        {
            _select = "count(*)";
            GetCommand().CommandText = GetSql();
            return (int) _command.ExecuteScalar();
        }

        public IReadOnlyCollection<long> ToIdList()
        {
            _select = "[Id]";
            GetCommand().CommandText = GetSql();

            using (var reader = ExecuteReader())
            {
                var idOrdinal = reader.GetOrdinal("Id");

                var result = new List<long>();

                while (reader.Read())
                {
                    result.Add(reader.GetInt64(idOrdinal));
                }

                _command.Dispose();
                _command = null;

                return result;
            }
        }

        public T First<T>(string selectText = null) where T : class
        {
            if (!string.IsNullOrEmpty(selectText))
            {
                _select = selectText;
            }

            var ret = ExecuteWithLimit<T>(GetSql(), 1);

            if (ret.Count == 0)
            {
                throw new IndexOutOfRangeException();
            }

            return ret[0];
        }

        public T FirstOrDefault<T>(string selectText = null) where T : class
        {
            if (!string.IsNullOrEmpty(selectText))
            {
                _select = selectText;
            }

            var ret = ExecuteWithLimit<T>(GetSql(), 1);

            if (ret.Count == 0)
            {
                return default(T);
            }

            return ret[0];
        }

        public IQueryBuilder New()
        {
            return new QueryBuilder(_domainModel);
        }
    }
}
    