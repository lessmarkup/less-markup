/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.UserInterface.Model.Global
{
    [RecordModel(CollectionType = typeof(CollectionManager), TitleTextId = UserInterfaceTextIds.EditSite, EntityType = EntityType.Site)]
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

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter)
            {
                return domainModel.GetCollection<Site>().Select(s => s.SiteId);
            }

            public IQueryable<SiteModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return domainModel.GetCollection<Site>().Select(s => new SiteModel
                {
                    Enabled = s.Enabled,
                    Host = s.Host,
                    Name = s.Name,
                    SiteId = s.SiteId,
                    Title = s.Title
                });
            }

            public bool Filtered { get { return false; } }

            public SiteModel AddRecord(SiteModel record, bool returnObject)
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
                    _changeTracker.AddChange(site.SiteId, EntityType.Site, EntityChangeType.Added, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();

                    record.SiteId = site.SiteId;
                    return record;
                }
            }

            public SiteModel UpdateRecord(SiteModel record, bool returnObject)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var site = domainModel.GetCollection<Site>().Single(s => s.SiteId == record.SiteId);
                    site.Enabled = record.Enabled;
                    site.Host = record.Host;
                    site.Name = record.Name;
                    site.Title = record.Title;
                    _changeTracker.AddChange(record.SiteId, EntityType.Site, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                    return record;
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    foreach (var recordId in recordIds)
                    {
                        var site = domainModel.GetCollection<Site>().Single(s => s.SiteId == recordId);
                        domainModel.GetCollection<Site>().Remove(site);
                        _changeTracker.AddChange(recordId, EntityType.Site, EntityChangeType.Removed, domainModel);
                    }
                    domainModel.SaveChanges();
                    domainModel.CompleteTransaction();
                    return true;
                }
            }
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
