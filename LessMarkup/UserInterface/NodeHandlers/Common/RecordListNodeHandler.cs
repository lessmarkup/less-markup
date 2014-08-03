/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using LessMarkup.DataFramework;
using LessMarkup.Engine.Configuration;
using LessMarkup.Engine.Language;
using LessMarkup.Framework.Helpers;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Exceptions;
using LessMarkup.UserInterface.Model.RecordModel;
using Newtonsoft.Json;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public class RecordListNodeHandler<T> : AbstractNodeHandler where T : class
    {
        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly RecordModelDefinition _recordModel;
        private IModelCollection<T> _collection;
        private IEditableModelCollection<T> _editableCollection;
        private readonly PropertyInfo _idProperty;

        // ReSharper disable NotAccessedField.Local
        struct CellLink
        {
            public string Text;
            public string Link;
        }

        struct CellButton
        {
            public string Text;
            public string Command;
            public string VisibleCondition;
        }
        // ReSharper restore NotAccessedField.Local

        private readonly List<CellButton> _cellButtons = new List<CellButton>();
        private readonly List<CellLink> _cellLinks = new List<CellLink>();

        public RecordListNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache)
        {
            _domainModelProvider = domainModelProvider;
            _dataCache = dataCache;

            var formCache = dataCache.Get<RecordModelCache>();
            _recordModel = formCache.GetDefinition(typeof(T));

            if (_recordModel == null)
            {
                throw new ArgumentException(LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.MissingParameter, typeof(T).FullName));
            }

            _idProperty = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).First(p => p.Name.EndsWith("Id"));
        }

        private IEditableModelCollection<T> GetEditableCollection()
        {
            InitializeCollections();
            return _editableCollection;
        }

        private IModelCollection<T> GetCollection()
        {
            InitializeCollections();
            return _collection;
        }

        private void InitializeCollections()
        {
            if (_collection != null)
            {
                return;
            }

            _collection = CreateCollection();
            _editableCollection = _collection as IEditableModelCollection<T>;
            _collection.Initialize(ObjectId, AccessType);

            if (_collection == null)
            {
                throw new ArgumentException(LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.MissingParameter, typeof(T).FullName));
            }
        }

        protected virtual IModelCollection<T> CreateCollection()
        {
            return (IModelCollection<T>) DependencyResolver.Resolve(_recordModel.CollectionType);
        }

        protected override string ViewType
        {
            get { return "RecordList"; }
        }

        private static string GetColumnWidth(ColumnDefinition column)
        {
            if (column.WidthPixels.HasValue)
            {
                return column.WidthPixels.Value.ToString(CultureInfo.InvariantCulture);
            }
            if (column.WidthPercents.HasValue)
            {
                return column.WidthPercents.Value + "%";
            }
            if (column.WidthWeight.HasValue)
            {
                return new String('*', column.WidthWeight.Value);
            }
            return "*";
        }

        private static string GetCellFilter(ColumnDefinition column)
        {
            if (column.Property.PropertyType == typeof(DateTime))
            {
                return "date:\'medium\'";
            }
            return "";
        }

        protected void AddCellButton(string text, string commandId, string visibleCondition = null)
        {
            _cellButtons.Add(new CellButton { Text = text, Command = commandId, VisibleCondition = visibleCondition});
        }

        protected void AddCellLink(string text, string link)
        {
            _cellLinks.Add(new CellLink { Text = text, Link = link });
        }

        protected override object GetViewData()
        {
            var siteConfiguration = _dataCache.Get<SiteConfigurationCache>();
            var recordsPerPage = siteConfiguration.RecordsPerPage;

            var definition = _dataCache.Get<RecordModelCache>().GetDefinition(typeof(T));

            var recordsActions = new List<Tuple<string, string>>();

            foreach (var method in GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                var actionAttribute = method.GetCustomAttribute<RecordActionAttribute>();
                if (actionAttribute == null)
                {
                    continue;
                }

                var parameters = method.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof (List<long>))
                {
                    continue;
                }

                recordsActions.Add(Tuple.Create(LanguageHelper.GetText(definition.ModuleType, actionAttribute.NameTextId),
                    string.IsNullOrEmpty(actionAttribute.Action) ? method.Name : actionAttribute.Action));
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var recordIds = GetCollection().ReadIds(domainModel, null);

                var data = new
                {
                    Editable = _editableCollection != null,
                    DeleteOnly = _editableCollection != null && _editableCollection.DeleteOnly,
                    PerPage = recordsPerPage,
                    Type = definition.Id,
                    RecordIds = recordIds.ToList(),
                    Live = true,
                    RecordId = _idProperty.Name,
                    RefreshOnAllActions,
                    Actions = recordsActions.Select(a => new { Name = a.Item1, Action = a.Item2}).ToList(),
                    Columns = _recordModel.Columns.Select(c => new
                    {
                        width = GetColumnWidth(c),
                        field = c.Property.Name,
                        displayName = LanguageHelper.GetText(definition.ModuleType, c.TextId),
                        maxWidth = c.MaxWidth,
                        minWidth = c.MinWidth,
                        visible = c.Visible,
                        sortable = c.Sortable,
                        resizable = c.Resizable,
                        pinnable = c.Pinnable,
                        cellClass = c.CellClass,
                        headerClass = c.HeaderClass,
                        cellTemplate = c.CellTemplate,
                        cellFilter = GetCellFilter(c),
                    }).ToList(),
                    CellCommands = _cellButtons,
                    CellLinks = _cellLinks,
                    ColumnsResizable = true
                };

                return data;
            }
        }

        public virtual bool RefreshOnAllActions { get { return false; } }

        public object HandleRecordsAction(List<long> ids, string action)
        {
            var method = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).SingleOrDefault(a => string.Compare(a.Name, action, StringComparison.InvariantCultureIgnoreCase) == 0);

            if (method == null || method.GetParameters().Length != 1 || method.GetParameters()[0].ParameterType != typeof(List<long>))
            {
                throw new UnknownActionException();
            }

            return method.Invoke(this, new object[] {ids});
        }

        private int GetIndex(object modifiedObject, string filter)
        {
            int index;

            using (var domainModel = _domainModelProvider.Create())
            {
                var recordIds = GetCollection().ReadIds(domainModel, filter).ToList();
                var recordId = (long)_idProperty.GetValue(modifiedObject);
                index = recordIds.IndexOf(recordId);
            }

            return index;
        }

        public object ModifyRecord(string objectToModify, string filter)
        {
            var typedObjectToAdd = JsonConvert.DeserializeObject<T>(objectToModify);

            _recordModel.ValidateInput(typedObjectToAdd, false);

            GetEditableCollection().UpdateRecord(typedObjectToAdd);

            return new
            {
                Index = GetIndex(typedObjectToAdd, filter),
                Record = typedObjectToAdd
            };
        }

        public object AddRecord(string objectToAdd, string filter)
        {
            var typedObjectToAdd = JsonConvert.DeserializeObject<T>(objectToAdd);

            _recordModel.ValidateInput(typedObjectToAdd, true);

            GetEditableCollection().AddRecord(typedObjectToAdd);

            return new
            {
                Index = GetIndex(typedObjectToAdd, filter),
                Record = typedObjectToAdd
            };
        }

        public object RemoveRecords(List<long> recordIds)
        {
            return GetEditableCollection().DeleteRecords(recordIds);
        }

        public object Fetch(List<long> ids)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var records = GetCollection().Read(domainModel, ids).ToList();

                return new
                {
                    Records = records
                };
            }
        }

        protected virtual T RecordCommand(long recordId, string commandId)
        {
            return null;
        }

        public object CellCommand(long recordId, string commandId, string filter)
        {
            T ret = RecordCommand(recordId, commandId);

            if (ret == null)
            {
                return new
                {

                };
            }

            return new
            {
                Record = ret,
                Index = GetIndex(ret, filter)
            };
        }

        protected override string[] Scripts
        {
            get { return new [] {"controllers/recordlist"}; }
        }
    }
}
