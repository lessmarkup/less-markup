/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.Engine.Logging;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.Engine.TextAndSearch
{
    class TextSearchEngine : ITextSearch
    {
        private readonly List<TableSearch> _tables = new List<TableSearch>();
        private int _textFieldsCount;

        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IModuleIntegration _moduleIntegration;

        private readonly object _initLock = new object();
        private bool _initialized;

        public const string FulltextSearchParameter = "param1";
        public const string LikeSearchParameter = "param2";

        public TextSearchEngine(IDomainModelProvider domainModelProvider, IModuleIntegration moduleIntegration)
        {
            _domainModelProvider = domainModelProvider;
            _moduleIntegration = moduleIntegration;
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            lock (_initLock)
            {
                if (_initialized)
                {
                    return;
                }

                _initialized = true;

                using (var model = _domainModelProvider.Create())
                {
                    using (var connection = model.Database.Connection)
                    {
                        connection.Open();

                        foreach (var collectionProperty in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            var collectionTypes = collectionProperty.PropertyType.GenericTypeArguments;

                            if (collectionTypes.Length != 1)
                            {
                                continue;
                            }

                            var type = collectionTypes[0];

                            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.GetCustomAttribute<TextSearchAttribute>() != null).Select(p => p.Name).ToList();

                            if (!properties.Any())
                            {
                                continue;
                            }

                            var idProperty = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                .FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);

                            if (idProperty == null)
                            {
                                this.LogDebug("Key property is not found for type '" + type.Name + "', ignoring type");
                                continue;
                            }

                            var updatedProperty = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(p => p.Name == "Created");
                            var createdProperty = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(p => p.Name == "Updated");

                            var entityAttribute = type.GetCustomAttribute<EntityAttribute>();
                            if (entityAttribute == null)
                            {
                                this.LogDebug("Entity attribute is not defined for type '" + type.Name + "', ignoring type");
                                continue;
                            }

                            var tableSearch = new TableSearch
                            {
                                Name = collectionProperty.Name, 
                                FullTextEnabled = true, 
                                Columns = properties, 
                                IdColumn = idProperty.Name, 
                                CollectionId = AbstractDomainModel.GetCollectionId(entityAttribute.CollectionType),
                                UpdatedColumn = updatedProperty != null ? updatedProperty.Name : null,
                                CreatedColumn = createdProperty != null ? createdProperty.Name : null
                            };
                            _tables.Add(tableSearch);

                            if (_textFieldsCount < properties.Count)
                            {
                                _textFieldsCount = properties.Count;
                            }

                            foreach (var property in properties)
                            {
                                var query = string.Format("SELECT COLUMNPROPERTY(object_id('{0}'), '{1}', 'IsFulltextIndexed')", tableSearch.Name, property);

                                using (var command = connection.CreateCommand())
                                {
                                    command.CommandText = query;
                                    command.CommandType = CommandType.Text;

                                    var result = command.ExecuteScalar();

                                    if (result == null || result == System.DBNull.Value || (int)result != 1)
                                    {
                                        tableSearch.FullTextEnabled = false;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var table in _tables)
                {
                    table.InitializeQuery(_textFieldsCount);
                }
            }
        }

        public List<SearchResult> Search(string text, int startRecord, int recordCount, IDomainModel domainModel)
        {
            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim(new[] { '%', '*', ' ', '.', ';', '-', '+', ',', ':', '(', ')', '[', ']', '?', '=', '<', '>', '\r', '\n', '\t' });
            }

            if (string.IsNullOrEmpty(text))
            {
                return new List<SearchResult>();
            }

            Initialize();

            var query = "";

            foreach (var table in _tables)
            {
                if (query.Length > 0)
                {
                    query += " union ";
                }

                query += table.Query;
            }

            query = string.Format(
                "select * from (select *, row_number() over (order by a.rank desc, a.updated desc) as rownum from ({0}) a) b where b.rownum >= {1} and b.rownum <= {2} order by rownum desc",
                query, startRecord+1, startRecord+1+recordCount);

            using (var connection = domainModel.Database.Connection)
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = query;
                command.CommandType = CommandType.Text;
                var parameter = command.CreateParameter();
                parameter.ParameterName = FulltextSearchParameter;
                parameter.Value = "*" + text + "*";
                command.Parameters.Add(parameter);
                parameter = command.CreateParameter();
                parameter.ParameterName = LikeSearchParameter;
                parameter.Value = "%" + text.ToUpper() + "%";
                command.Parameters.Add(parameter);

                var ret = new List<SearchResult>();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var result = new SearchResult
                        {
                            CollectionId = reader.GetInt32(2),
                            EntityId = reader.GetInt64(0),
                            Text = ""
                        };

                        for (var i = 0; i < _textFieldsCount; i++)
                        {
                            var fieldText = reader.GetValue(i + 4);

                            if (fieldText == null || fieldText == System.DBNull.Value || string.IsNullOrWhiteSpace((string)fieldText))
                            {
                                continue;
                            }

                            if (!string.IsNullOrEmpty(result.Text))
                            {
                                result.Text = result.Text + " / ";
                            }
                            result.Text += (string)fieldText;
                        }

                        ret.Add(_moduleIntegration.IsSearchResultValid(result) ? result : null);
                    }
                }

                return ret;
            }
        }
    }
}
