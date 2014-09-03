/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.UserInterface.ChangeTracking
{
    abstract class TrackerRecordType
    {
        public static TrackerRecordType Create(string connectionId, string modelId, object filter, IDataCache dataCache)
        {
            var modelDefinition = dataCache.Get<IRecordModelCache>().GetDefinition(modelId);
            var constructorInfo = typeof (TrackerRecordType<>).MakeGenericType(modelDefinition.DataType).GetConstructors().Single();
            return (TrackerRecordType) constructorInfo.Invoke(new[] {connectionId, modelDefinition.CollectionType, modelDefinition.CollectionType, filter});
        }

        public abstract int CollectionId { get; }

        public abstract void OnRecordChanged(long recordId, long entityId, EntityChangeType entityChange, IDomainModel domainModel);
        public abstract void GetAllIds(IDomainModelProvider domainModelProvider);
        public abstract void GetRecords(List<long> recordIds, IDomainModelProvider domainModelProvider);
    }

    class TrackerRecordType<T> : TrackerRecordType
    {
        private readonly int _collectionId;
        private readonly IModelCollection<T> _collectionManager;
        private readonly string _connectionId;
        private int _changeNumber;

        public string Filter { get; set; }

        public TrackerRecordType(string connectionId, Type collectionType, Type collectionManagerType, string filter)
        {
            _connectionId = connectionId;
            Filter = filter;
            _collectionId = AbstractDomainModel.GetCollectionIdVerified(collectionType);

            var managerInstance = DependencyResolver.Resolve(collectionManagerType);
            _collectionManager = (IModelCollection<T>) managerInstance;
        }

        public override int CollectionId
        {
            get { return _collectionId; }
        }

        private dynamic GetClient()
        {
            return null;//GlobalHost.ConnectionManager.GetHubContext<RecordListHub>().Clients.Client(_connectionId);
        }

        public override void GetAllIds(IDomainModelProvider domainModelProvider)
        {
            List<long> recordIds;

            using (var domainModel = domainModelProvider.Create())
            {
                recordIds = _collectionManager.ReadIds(domainModel, Filter, false).ToList();
            }

            var client = GetClient();
            client.RecordIds(++_changeNumber, recordIds);
        }

        public override void GetRecords(List<long> recordIds, IDomainModelProvider domainModelProvider)
        {
            List<T> records;

            using (var domainModel = domainModelProvider.Create())
            {
                records = _collectionManager.Read(domainModel, recordIds).ToList();
            }

            var client = GetClient();
            client.Records(++_changeNumber, records);
        }

        public override void OnRecordChanged(long recordId, long entityId, EntityChangeType entityChange, IDomainModel domainModel)
        {
            var client = GetClient();

            if (entityChange == EntityChangeType.Updated)
            {
                client.RecordUpdated(++_changeNumber, entityId);
                return;
            }

            if (entityChange == EntityChangeType.Removed)
            {
                client.RecordRemoved(++_changeNumber, entityId);
                return;
            }

            var newIds = _collectionManager.ReadIds(domainModel, Filter, false).ToList();
            var indexOf = newIds.IndexOf(entityId);
            if (indexOf < 0)
            {
                return;
            }

            client.RecordAdded(++_changeNumber, indexOf, entityId);
        }
    }
}
