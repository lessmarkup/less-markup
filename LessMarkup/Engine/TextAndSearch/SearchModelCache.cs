/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.Engine.TextAndSearch
{
    class SearchModelCache : AbstractCacheHandler, ITextSearch
    {
        private readonly List<TableSearchModel> _tableModels = new List<TableSearchModel>();
        private int _textFieldsCount;

        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IModuleIntegration _moduleIntegration;
        private readonly ISiteMapper _siteMapper;
        private readonly IHtmlSanitizer _htmlSanitizer;
        private static readonly char[] _excludedChars = { '%', '*', ' ', '.', ';', '-', '+', ',', ':', '(', ')', '[', ']', '?', '=', '<', '>', '\r', '\n', '\t' };

        public const string FulltextSearchParameter = "param1";
        public const string LikeSearchParameter = "param2";
        public const string SiteIdSearchParameter = "siteid";

        public SearchModelCache(IDomainModelProvider domainModelProvider, IModuleIntegration moduleIntegration, ISiteMapper siteMapper, IHtmlSanitizer htmlSanitizer) : base(new []{typeof(Interfaces.Data.Module)})
        {
            _domainModelProvider = domainModelProvider;
            _moduleIntegration = moduleIntegration;
            _siteMapper = siteMapper;
            _htmlSanitizer = htmlSanitizer;
        }

        private void HandleCollection(Type dataType, SqlConnection connection, PropertyInfo collectionProperty)
        {
            var allProperties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ToList();

            var properties = allProperties.Where(p => p.GetCustomAttribute<TextSearchAttribute>() != null).Select(p => p.Name).ToList();

            if (!properties.Any())
            {
                return;
            }

            var idProperty = allProperties.FirstOrDefault(p => p.Name == "Id");

            if (idProperty == null)
            {
                return;
            }

            var updatedProperty = allProperties.FirstOrDefault(p => p.Name == "Created");
            var createdProperty = allProperties.FirstOrDefault(p => p.Name == "Updated");
            var siteIdProperty = allProperties.FirstOrDefault(p => p.Name == "SiteId");

            if (siteIdProperty == null)
            {
                return;
            }

            var collectionId = DataHelper.GetCollectionId(dataType);

            var entitySearch = _moduleIntegration.GetEntitySearch(dataType);

            if (entitySearch == null)
            {
                return;
            }

            var searchModel = new TableSearchModel
            {
                Name = entitySearch.GetFriendlyName(collectionId),
                TableName = collectionProperty.Name,
                FullTextEnabled = true,
                Columns = properties,
                IdColumn = idProperty.Name,
                CollectionId = collectionId,
                UpdatedColumn = updatedProperty != null ? updatedProperty.Name : null,
                CreatedColumn = createdProperty != null ? createdProperty.Name : null,
                SiteIdColumn = siteIdProperty.Name,
                EntitySearch = entitySearch
            };

            _tableModels.Add(searchModel);

            if (_textFieldsCount < properties.Count)
            {
                _textFieldsCount = properties.Count;
            }

            foreach (var property in properties)
            {
                var query = string.Format("SELECT COLUMNPROPERTY(object_id('{0}'), '{1}', 'IsFulltextIndexed')", searchModel.TableName, property);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.CommandType = CommandType.Text;

                    var result = command.ExecuteScalar();

                    if (result == null || result == DBNull.Value || (int)result != 1)
                    {
                        // All searchable properties should support full text search to work with full text
                        searchModel.FullTextEnabled = false;
                        break;
                    }
                }
            }
        }

        public SearchResults Search(string text, int startRecord, int recordCount, IDomainModel domainModel)
        {
            if (_tableModels.Count == 0)
            {
                return null;
            }

            var siteId = _siteMapper.SiteId;

            if (!siteId.HasValue)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(text))
            {
                text = text.Trim(_excludedChars);
            }

            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var query = "";

            var ret = new SearchResults
            {
                ActualCount = 0,
                Results = new List<SearchResult>()
            };

            foreach (var table in _tableModels)
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

                var words = text.Split(new[] {' ', '\'', '\"'});

                parameter.Value = "" + string.Join(" AND ", words) + "";
                command.Parameters.Add(parameter);
                parameter = command.CreateParameter();
                parameter.ParameterName = LikeSearchParameter;
                parameter.Value = "%" + text.ToUpper() + "%";
                command.Parameters.Add(parameter);
                parameter = command.CreateParameter();
                parameter.ParameterName = SiteIdSearchParameter;
                parameter.Value = siteId.Value;
                command.Parameters.Add(parameter);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var collectionId = reader.GetInt32(2);
                        var entityId = reader.GetInt64(0);

                        var tableModel = _tableModels.First(m => m.CollectionId == collectionId);

                        ret.ActualCount++;

                        var url = tableModel.EntitySearch.ValidateAndGetUrl(collectionId, entityId, domainModel);

                        if (url == null)
                        {
                            continue;
                        }

                        var result = new SearchResult
                        {
                            Text = "",
                            Url = url,
                            Name = tableModel.Name
                        };

                        for (var i = 0; i < _textFieldsCount; i++)
                        {
                            var fieldText = reader.GetValue(i + 4);

                            if (fieldText == null || fieldText == DBNull.Value || string.IsNullOrWhiteSpace((string)fieldText))
                            {
                                continue;
                            }

                            fieldText = _htmlSanitizer.ExtractText((string)fieldText);

                            if (!string.IsNullOrEmpty(result.Text))
                            {
                                result.Text = result.Text + " / ";
                            }
                            result.Text += (string)fieldText;
                        }

                        var indexOf = result.Text.IndexOf(text, StringComparison.InvariantCultureIgnoreCase);

                        if (indexOf > 50)
                        {
                            var start = indexOf - 50;
                            while (start > 0)
                            {
                                if (char.IsSeparator(result.Text[start]))
                                {
                                    start++;
                                    break;
                                }

                                start--;
                            }

                            if (start > 0)
                            {
                                result.Text = result.Text.Substring(start);
                            }
                        }

                        ret.Results.Add(result);
                    }
                }

                return ret;
            }
        }

        protected override void Initialize(long? siteId, long? objectId)
        {
            using (var model = _domainModelProvider.Create())
            {
                using (var connection = (SqlConnection) model.Database.Connection)
                {
                    connection.Open();

                    foreach (var collectionProperty in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var propertyType = collectionProperty.PropertyType;

                        if (!propertyType.IsGenericType || propertyType.GetGenericTypeDefinition() != typeof (DbSet<>))
                        {
                            continue;
                        }

                        var collectionTypes = propertyType.GenericTypeArguments;

                        if (collectionTypes.Length != 1)
                        {
                            continue;
                        }

                        var type = collectionTypes[0];

                        if (!typeof (IDataObject).IsAssignableFrom(type))
                        {
                            continue;
                        }

                        HandleCollection(type, connection, collectionProperty);
                    }
                }
            }

            foreach (var table in _tableModels)
            {
                table.Initialize(_textFieldsCount);
            }
        }

        protected override bool Expires(int collectionId, long entityId, EntityChangeType changeType)
        {
            return true;
        }
    }
}
