/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LessMarkup.DataFramework;
using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public class RecordListNodeHandler<T> : AbstractNodeHandler where T : class
    {
        class Link
        {
            public string Text { get; set; }
            public string Url { get; set; }
            public bool External { get; set; }
        }

        enum ActionType
        {
            Record, // Is assigned to each record
            Create, // To show new type create dialog (like new record or new forum thread etc)
            RecordCreate, // To show new type create dialog associated with existing record
            RecordInitializeCreate, // To show new type create dialog associated with existing record, with pre-initialization
        }

        class Action
        {
            public string Text { get; set; }
            public string Name { get; set; }
            public string Visible { get; set; }
            public string Parameter { get; set; }
            public ActionType Type { get; set; }
        }

        private readonly Dictionary<string, string> _columnUrls = new Dictionary<string, string>(); 
        private readonly List<Link> _links = new List<Link>();
        private readonly List<Action> _actions = new List<Action>(); 
        private readonly PropertyInfo _idProperty;
        private IModelCollection<T> _collection;
        private IEditableModelCollection<T> _editableCollection;
        private readonly IRecordModelDefinition _recordModel;
        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly ICurrentUser _currentUser;

        protected PropertyInfo IdProperty { get { return _idProperty; } }

        protected IRecordModelDefinition RecordModel { get { return _recordModel; } }

        public RecordListNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser)
        {
            _domainModelProvider = domainModelProvider;
            _dataCache = dataCache;
            _currentUser = currentUser;

            var formCache = dataCache.Get<IRecordModelCache>();
            _recordModel = formCache.GetDefinition(typeof(T));

            if (_recordModel == null)
            {
                throw new ArgumentException(LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.MissingParameter, typeof(T).FullName));
            }

            _idProperty = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).First(p => p.Name.EndsWith("Id"));
        }

        protected virtual void AddEditActions()
        {
            if (_editableCollection != null)
            {
                if (_editableCollection.DeleteOnly)
                {
                    AddRecordAction("RemoveRecord", Constants.ModuleType.UserInterface, UserInterfaceTextIds.RemoveRecord);
                }
                else
                {
                    AddCreateAction("AddRecord", Constants.ModuleType.UserInterface, UserInterfaceTextIds.AddRecord, typeof(T));
                    AddRecordAction("ModifyRecord", Constants.ModuleType.UserInterface, UserInterfaceTextIds.ModifyRecord);
                    AddRecordAction("RemoveRecord", Constants.ModuleType.UserInterface, UserInterfaceTextIds.RemoveRecord);
                }
            }
        }

        protected override object Initialize(object controller)
        {
            InitializeCollections();

            AddEditActions();

            var modelCache = _dataCache.Get<IRecordModelCache>();

            foreach (var method in GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                var actionAttribute = method.GetCustomAttribute<RecordActionAttribute>();
                if (actionAttribute == null)
                {
                    continue;
                }

                if (actionAttribute.MinimumAccess != NodeAccessType.NoAccess)
                {
                    if ((int) AccessType < (int) actionAttribute.MinimumAccess)
                    {
                        continue;
                    }
                }

                var parameters = method.GetParameters();
                if (parameters.Length < 1 || parameters[0].ParameterType != typeof(long))
                {
                    continue;
                }

                var action = new Action
                {
                    Text = LanguageHelper.GetText(_recordModel.ModuleType, actionAttribute.NameTextId),
                    Name = string.IsNullOrEmpty(actionAttribute.Action) ? method.Name : actionAttribute.Action,
                    Visible = actionAttribute.Visible,
                    Type = actionAttribute.CreateType != null ? ActionType.RecordCreate : ActionType.Record
                };

                if (actionAttribute.CreateType != null)
                {
                    action.Type = actionAttribute.Initialize ? ActionType.RecordInitializeCreate : ActionType.RecordCreate;
                    action.Parameter = modelCache.GetDefinition(actionAttribute.CreateType).Id;
                }

                _actions.Add(action);
            }

            return null;
        }

        protected IEditableModelCollection<T> GetEditableCollection()
        {
            return _editableCollection;
        }

        protected IModelCollection<T> GetCollection()
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

        protected virtual bool SupportsLiveUpdates
        {
            get { return true; }
        }

        protected virtual bool SupportsManualRefresh
        {
            get { return false; }
        }

        private static string GetColumnWidth(ColumnDefinition column)
        {
            if (!string.IsNullOrEmpty(column.Width))
            {
                return column.Width;
            }
            return "*";
        }

        protected void AddRecordLink(object text, string url, bool external = false)
        {
            _links.Add(new Link { Text = LanguageHelper.GetText(_recordModel.ModuleType, text), Url = url, External = external});
        }

        protected void AddRecordAction(string name, string moduleType, object text, string visible = null)
        {
            _actions.Add(new Action { Name = name, Text = LanguageHelper.GetText(moduleType, text), Visible = visible, Type = ActionType.Record});
        }

        protected void AddCreateAction<TM>(string name, string moduleType, object text)
        {
            AddCreateAction(name, moduleType, text, typeof(TM));
        }

        protected void AddCreateAction(string name, string moduleType, object text, Type type)
        {
            var typeId = _dataCache.Get<IRecordModelCache>().GetDefinition(type).Id;
            _actions.Add(new Action { Name = name, Text = LanguageHelper.GetText(moduleType, text), Type = ActionType.Create, Parameter = typeId });
        }

        protected void AddRecordColumnLink(string field, string url)
        {
            _columnUrls[field] = url;
        }

        protected string GetColumnLink(string field)
        {
            string ret;
            _columnUrls.TryGetValue(field, out ret);
            return ret;
        }

        protected override Dictionary<string, object> GetViewData()
        {
            var siteConfiguration = _dataCache.Get<ISiteConfiguration>();
            var recordsPerPage = siteConfiguration.RecordsPerPage;
            var resourceCache = _dataCache.Get<IResourceCache>(_dataCache.Get<ILanguageCache>().CurrentLanguageId);

            using (var domainModel = _domainModelProvider.Create())
            {
                var data = new Dictionary<string, object>
                {
                    { "recordsPerPage", recordsPerPage },
                    { "type", _recordModel.Id },
                    { "extensionScript", ExtensionScript },
                    { "recordId", _idProperty.Name.ToJsonCase() },
                    { "liveUpdates", SupportsLiveUpdates },
                    { "manualRefresh", SupportsManualRefresh },
                    { "hasSearch", typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(p => p.GetCustomAttribute<RecordSearchAttribute>() != null && p.PropertyType == typeof(string))},
                    { "actions", _actions.Select(a => new
                    {
                        name = a.Name, 
                        text = a.Text, 
                        visible = a.Visible,
                        type = a.Type,
                        parameter = a.Parameter
                    }) },
                    { "links", _links.Select(l => new { text = l.Text, url = l.Url, external = l.External }) },
                    { "optionsTemplate", resourceCache.ReadText("~/Views/RecordOptions.html") },
                    { "columns", _recordModel.Columns.Select(c => new
                    {
                        width = GetColumnWidth(c),
                        name = c.Property.Name.ToJsonCase(),
                        text = LanguageHelper.GetText(_recordModel.ModuleType, c.TextId),
                        url = GetColumnLink(c.Property.Name) ?? c.CellUrl,
                        sortable = c.Sortable,
                        template = c.CellTemplate,
                        cellClick = c.CellUrl,
                        allowUnsafe = c.AllowUnsafe,
                        cellClass = c.CellClass,
                        headerClass = c.HeaderClass,
                        scope = c.Scope,
                        align = c.Align,
                        ignoreOptions = c.IgnoreOptions
                    }).ToList() },
                };

                ReadRecordsAndIds(data, domainModel, recordsPerPage);

                return data;
            }
        }

        protected int GetIndex(T modifiedObject, string filter, IDomainModel domainModel)
        {
            var recordIds = GetCollection().ReadIds(domainModel, filter, false).ToList();
            var recordId = (long)_idProperty.GetValue(modifiedObject);
            return recordIds.IndexOf(recordId);
        }

        protected int GetIndex(T modifiedObject, string filter)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                return GetIndex(modifiedObject, filter, domainModel);
            }
        }

        public object ModifyRecord(T modifiedObject, string filter, string rawModifiedObject)
        {
            _recordModel.ValidateInput(modifiedObject, false, rawModifiedObject);

            GetEditableCollection().UpdateRecord(modifiedObject);

            return ReturnRecordResult(modifiedObject, false, GetIndex(modifiedObject, filter));
        }

        public object CreateRecord()
        {
            var collection = GetEditableCollection();

            return new
            {
                record = collection != null ? collection.CreateRecord() : DependencyResolver.Resolve<T>()
            };
        }

        public object AddRecord(T newObject, string filter, Dictionary<string, string> settings, string rawNewObject)
        {
            if (newObject == null)
            {
                return ReturnRecordResult(DependencyResolver.Resolve<T>());
            }

            _recordModel.ValidateInput(newObject, true, rawNewObject);

            GetEditableCollection().AddRecord(newObject);

            return ReturnRecordResult(newObject, true, GetIndex(newObject, filter));
        }

        public object RemoveRecord(long recordId, string filter)
        {
            GetEditableCollection().DeleteRecords(new []{recordId});

            return ReturnRemovedResult();
        }

        public Dictionary<string, object> GetRecordIds(string filter)
        {
            var collection = GetCollection();
            List<long> recordIds;

            using (var domainModel = _domainModelProvider.Create())
            {
                recordIds = collection.ReadIds(domainModel, filter, false).ToList();
            }

            return new Dictionary<string, object>
            {
                {"recordIds", recordIds}
            };
        }

        protected override bool ProcessUpdates(long? fromVersion, long toVersion, Dictionary<string, object> returnValues, IDomainModel domainModel, Dictionary<string, object> arguments)
        {
            var collection = GetCollection();
            var changesCache = _dataCache.Get<IChangesCache>();
            var changes = changesCache.GetCollectionChanges(collection.CollectionId, fromVersion, toVersion);

            if (changes == null)
            {
                return false;
            }

            var removed = new List<long>();
            var updated = new List<long>();

            string filter = null;
            object filterObj;
            if (arguments.TryGetValue("filter", out filterObj) && filterObj != null)
            {
                filter = filterObj.ToString();
            }

            List<long> recordIds = null;

            foreach (var change in changes)
            {
                if (recordIds == null)
                {
                    recordIds = collection.ReadIds(domainModel, filter, false).ToList();
                }

                if (!recordIds.Contains(change.EntityId))
                {
                    if (removed.Count == 0)
                    {
                        returnValues["records_removed"] = removed;
                    }

                    removed.Add(change.EntityId);
                }
                else
                {
                    if (updated.Count == 0)
                    {
                        returnValues["records_updated"] = updated;
                    }

                    updated.Add(change.EntityId);
                }
            }

            return removed.Count > 0 || updated.Count > 0;
        }

        private void ReadRecordsAndIds(Dictionary<string, object> values, IDomainModel domainModel, int recordsPerPage)
        {
            var collection = GetCollection();

            var recordIds = collection.ReadIds(domainModel, null, false).ToList();
            values["recordIds"] = recordIds;

            if (recordsPerPage > 0)
            {
                var readRecordIds = recordIds.Take(recordsPerPage).ToList();
                ReadRecords(values, readRecordIds, domainModel);
            }
        }

        protected virtual void ReadRecords(Dictionary<string, object> values, List<long> ids, IDomainModel domainModel)
        {
            var records = GetCollection().Read(domainModel, ids).ToList();

            foreach (var record in records)
            {
                PostProcessRecord(record);
            }

            values["records"] = records;
        }

        protected virtual void PostProcessRecord(T record)
        {
        }

        protected Dictionary<string, object> ReturnRemovedResult()
        {
            return new Dictionary<string, object>
            {
                { "removed", true }
            };
        }

        protected Dictionary<string, object> ReturnRecordResult(long recordId, bool isNew = false, int index = -1)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var record = GetCollection().Read(domainModel, new List<long> {recordId}).First();
                return ReturnRecordResult(record, isNew, index);
            }
        }

        protected Dictionary<string, object> ReturnRecordResult(T record, bool isNew = false, int index = -1)
        {
            PostProcessRecord(record);

            return new Dictionary<string, object>
            {
                { "record", record },
                { "isNew", isNew },
                { "index", index }
            };
        }

        protected Dictionary<string, object> ReturnMessageResult(string message)
        {
            return new Dictionary<string, object>
            {
                { "message", message }
            };
        }

        protected Dictionary<string, object> ReturnResetResult()
        {
            return new Dictionary<string, object>
            {
                {"reset", true}
            };
        }

        protected Dictionary<string, object> ReturnRedirectResult(string url)
        {
            return new Dictionary<string, object>
            {
                { "redirect", url }
            };
        }

        public object Fetch(List<long> ids)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var ret = new Dictionary<string, object>();
                ReadRecords(ret, ids, domainModel);
                return ret;
            }
        }

        protected virtual string ExtensionScript { get { return null; } }
    }
}
