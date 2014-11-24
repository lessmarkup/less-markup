using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataFramework.Light
{
    public class QueryBuilder : ILightQueryBuilder
    {
        private readonly LightDomainModel _domainModel;
        private IDbCommand _command;
        private string _commandText;
        private int _parameterIndex;
        private string _select = "*";
        private string _where = "";
        private string _orderBy = "";
        private string _limit = "";

        internal QueryBuilder(LightDomainModel domainModel)
        {
            _domainModel = domainModel;
        }

        private IDataReader ExecuteReader()
        {
            try
            {
                return _command.ExecuteReader();
            }
            catch (Exception e)
            {
#if DEBUG
                Trace.WriteLine(e.Message);
#endif
                throw;
            }
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

        public ILightQueryBuilder From<T>(string name = null) where T : IDataObject
        {
            var metadata = LightDomainModel.GetMetadata<T>();

            _commandText += string.Format(" FROM [{0}] {1}", metadata.Name, name ?? "");

            return this;
        }

        public ILightQueryBuilder Join<T>(string name, string @on) where T : IDataObject
        {
            var metadata = LightDomainModel.GetMetadata<T>();

            _commandText += string.Format(" JOIN [{0}] {1} ON {2}", metadata.Name, name, @on);

            return this;
        }

        public ILightQueryBuilder LeftJoin<T>(string name, string @on) where T : IDataObject
        {
            var metadata = LightDomainModel.GetMetadata<T>();

            _commandText += string.Format(" LEFT JOIN [{0}] {1} ON {2}", metadata.Name, name, @on);

            return this;
        }

        public ILightQueryBuilder RightJoin<T>(string name, string @on) where T : IDataObject
        {
            var metadata = LightDomainModel.GetMetadata<T>();

            _commandText += string.Format(" RIGHT JOIN [{0}] {1} ON {2}", metadata.Name, name, @on);

            return this;
        }

        public ILightQueryBuilder GroupBy(string name)
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

        public ILightQueryBuilder Where(string filter, params object[] args)
        {
            foreach (var arg in args)
            {
                var pos = filter.IndexOf('$');
                if (pos < 0)
                {
                    break;
                }
                var name = string.Format("@_p{0}", _parameterIndex++);
                var command = GetCommand();
                var parameter = command.CreateParameter();
                parameter.DbType = GetDbType(arg.GetType());
                if (parameter.DbType == DbType.String || parameter.DbType == (DbType) SqlDbType.VarBinary)
                {
                    parameter.Size = -1;
                }
                parameter.Value = arg;
                parameter.ParameterName = name;
                command.Parameters.Add(parameter);
                filter = filter.Substring(0, pos) + name + filter.Substring(pos + 1);
            }

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

        public ILightQueryBuilder WhereIds(IEnumerable<long> ids)
        {
            return Where(string.Format("Id IN ({0})", string.Join(",", ids)));
        }

        public ILightQueryBuilder OrderBy(string column)
        {
            if (_orderBy.Length > 0)
            {
                _orderBy += string.Format(", [{0}]", column);
            }
            else
            {
                _orderBy = string.Format("ORDER BY [{0}]", column);
            }
            return this;
        }

        public ILightQueryBuilder OrderByDescending(string column)
        {
            if (_orderBy.Length > 0)
            {
                _orderBy = string.Format(", [{0}] DESC", column);
            }
            else
            {
                _orderBy = string.Format("ORDER BY [{0}] DESC", column);
            }
            return this;
        }

        public ILightQueryBuilder Limit(int @from, int count)
        {
            _limit = string.Format("OFFSET {0} ROWS FETCH NEXT {1} ROWS ONLY", @from, count);
            return this;
        }

        public T Find<T>(long id) where T : IDataObject
        {
            if (string.IsNullOrEmpty(_commandText))
            {
                From<T>();
            }

            return Where("Id = $", id).First<T>();
        }

        public T FindOrDefault<T>(long id) where T : IDataObject
        {
            if (string.IsNullOrEmpty(_commandText))
            {
                From<T>();
            }

            return Where("Id = $", id).FirstOrDefault<T>();
        }

        private List<T> ExecuteWithLimit<T>(string sql, int? limit, params object[] args)
        {
            var command = GetCommand();

            foreach (var arg in args)
            {
                var pos = sql.IndexOf('$');
                if (pos < 0)
                {
                    break;
                }
                var name = string.Format("@_x{0}", _parameterIndex++);
                var parameter = command.CreateParameter();
                parameter.DbType = GetDbType(arg.GetType());
                if (parameter.DbType == DbType.String || parameter.DbType == (DbType)SqlDbType.VarBinary)
                {
                    parameter.Size = -1;
                }
                parameter.Value = arg;
                parameter.ParameterName = name;
                command.Parameters.Add(parameter);
                sql = sql.Substring(0, pos) + name + sql.Substring(pos + 1);
            }

            command.CommandText = sql;

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
                    var obj = Interfaces.DependencyResolver.Resolve<T>();
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

        public List<T> Execute<T>(string sql, params object[] args)
        {
            return ExecuteWithLimit<T>(sql, null, args);
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

        public List<T> ToList<T>(string selectText = null)
        {
            if (!string.IsNullOrEmpty(selectText))
            {
                _select = selectText;
            }

            return Execute<T>(GetSql());
        }

        public IReadOnlyCollection<long> ToIdList()
        {
            _select = "Id";
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

        public T First<T>(string selectText = null)
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

        public T FirstOrDefault<T>(string selectText = null)
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

        public ILightQueryBuilder New()
        {
            return new QueryBuilder(_domainModel);
        }
    }
}
