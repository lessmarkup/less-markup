/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.UserInterface.Model.Global
{
    [RecordModel(CollectionType = typeof(CollectionManager), TitleTextId = UserInterfaceTextIds.EditSite, DataType = typeof(Site))]
    public class SiteModel
    {
        public class CollectionManager : IEditableModelCollection<SiteModel>
        {
            private readonly IDomainModelProvider _domainModelProvider;
            private readonly IChangeTracker _changeTracker;

            public CollectionManager(IDomainModelProvider domainModelProvider, IChangeTracker changeTracker)
            {
                _domainModelProvider = domainModelProvider;
                _changeTracker = changeTracker;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder)
            {
                return domainModel.GetCollection<Site>().Select(s => s.Id);
            }

            public int CollectionId { get { return AbstractDomainModel.GetCollectionId<Site>(); } }

            public IQueryable<SiteModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return domainModel.GetCollection<Site>().Select(s => new SiteModel
                {
                    Enabled = s.Enabled,
                    Host = s.Host,
                    Name = s.Name,
                    SiteId = s.Id,
                    Title = s.Title
                });
            }

            public bool Filtered { get { return false; } }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
            }

            public SiteModel CreateRecord()
            {
                return new SiteModel();
            }

            public void AddRecord(SiteModel record)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var site = new Site
                    {
                        Created = DateTime.UtcNow,
                        Enabled = record.Enabled,
                        Host = record.Host,
                        Name = record.Name,
                        Title = record.Title
                    };

                    domainModel.GetCollection<Site>().Add(site);
                    domainModel.SaveChanges();
                    _changeTracker.AddChange(site, EntityChangeType.Added, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();

                    record.SiteId = site.Id;
                }
            }

            public void UpdateRecord(SiteModel record)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var site = domainModel.GetCollection<Site>().Single(s => s.Id == record.SiteId);
                    site.Enabled = record.Enabled;
                    site.Host = record.Host;
                    site.Name = record.Name;
                    site.Title = record.Title;
                    _changeTracker.AddChange(site, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    foreach (var recordId in recordIds)
                    {
                        var site = domainModel.GetCollection<Site>().Single(s => s.Id == recordId);
                        domainModel.GetCollection<Site>().Remove(site);
                        _changeTracker.AddChange(site, EntityChangeType.Removed, domainModel);
                    }
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                    return true;
                }
            }

            public bool DeleteOnly { get { return false; } }
        }

        [Column(UserInterfaceTextIds.SiteId)]
        public long SiteId { get; set; }

        [Column(UserInterfaceTextIds.Name)]
        [InputField(InputFieldType.Text, UserInterfaceTextIds.Name, Required = true)]
        public string Name { get; set; }

        [Column(UserInterfaceTextIds.Title)]
        [InputField(InputFieldType.Text, UserInterfaceTextIds.Title)]
        public string Title { get; set; }

        [Column(UserInterfaceTextIds.Host)]
        [InputField(InputFieldType.Text, UserInterfaceTextIds.Host, Required = true)]
        public string Host { get; set; }

        [Column(UserInterfaceTextIds.Enabled)]
        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.Enabled)]
        public bool Enabled { get; set; }
    }
}
