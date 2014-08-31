/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Data.Entity;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataFramework.DataAccess
{
    class DomainModelStub : IDomainModel
    {
        public void Dispose()
        {
        }

        public void AddObject<T>(T newObject) where T : class, INonSiteDataObject
        {
        }

        public void AddSiteObject<T>(T newObject) where T : class, ISiteDataObject
        {
        }

        public void RemoveObject<T>(T obj) where T : class, INonSiteDataObject
        {
        }

        public void RemoveSiteObject<T>(T obj) where T : class, ISiteDataObject
        {
        }

        public IDataObjectCollection<T> GetSiteCollection<T>() where T : class, ISiteDataObject
        {
            return new DataObjectCollectionStub<T>();
        }

        public IDataObjectCollection<T> GetCollection<T>() where T : class, INonSiteDataObject
        {
            return new DataObjectCollectionStub<T>();
        }

        public IDataObjectCollection<T> GetSiteCollection<T>(long? siteId, bool rawCollection = false) where T : class, ISiteDataObject
        {
            return new DataObjectCollectionStub<T>();
        }

        public int SaveChanges()
        {
            return 0;
        }

        public Database Database { get { return null; } }

        public void CreateTransaction()
        {
        }

        public void CompleteTransaction()
        {
        }

        public void OnHistoryChanged()
        {
        }

        public void ImprovePerformance()
        {
        }

        public bool AutoDetectChangesEnabled { get; set; }
    }
}
