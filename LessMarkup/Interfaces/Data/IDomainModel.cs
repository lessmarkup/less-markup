/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Data.Entity;

namespace LessMarkup.Interfaces.Data
{
    public interface IDomainModel : IDisposable
    {
        void AddObject<T>(T newObject) where T : class, INonSiteDataObject;
        void AddSiteObject<T>(T newObject) where T : class, ISiteDataObject;
        IDataObjectCollection<T> GetSiteCollection<T>() where T : class, ISiteDataObject;
        IDataObjectCollection<T> GetCollection<T>() where T : class, INonSiteDataObject;
        IDataObjectCollection<T> GetSiteCollection<T>(long? siteId, bool rawCollection = false) where T : class, ISiteDataObject;
        int SaveChanges();
        Database Database { get; }
        void CreateTransaction();
        void CompleteTransaction();
        void ImprovePerformance();
    }
}
