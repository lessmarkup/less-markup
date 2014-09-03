/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.DataObjects.Common;
using LessMarkup.Engine.Language;
using LessMarkup.Framework;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.MainModule.Model
{
    [RecordModel(CollectionType = typeof(Collection), TitleTextId = MainModuleTextIds.ViewTestMessage)]
    public class TestMessageModel
    {
        public class Collection : IEditableModelCollection<TestMessageModel>
        {
            private readonly IDomainModelProvider _domainModelProvider;

            public Collection(IDomainModelProvider domainModelProvider)
            {
                _domainModelProvider = domainModelProvider;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder)
            {
                var query = (IQueryable<TestMail>) domainModel.GetSiteCollection<TestMail>();

                if (!ignoreOrder)
                {
                    query = query.OrderByDescending(e => e.Sent);
                }

                return query.Select(e => e.Id);
            }

            public int CollectionId { get { return AbstractDomainModel.GetCollectionIdVerified<TestMail>(); } }

            public IQueryable<TestMessageModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return domainModel.GetSiteCollection<TestMail>().Where(e => ids.Contains(e.Id)).Select(e => new TestMessageModel
                {
                    Body = e.Body,
                    From = e.From,
                    Sent = e.Sent,
                    Subject = e.Subject,
                    Template = e.Template,
                    To = e.To,
                    TestMailId = e.Id
                });
            }

            public bool Filtered { get { return false; } }

            public void Initialize(long? objectId, NodeAccessType nodeAccessType)
            {
            }

            public TestMessageModel CreateRecord()
            {
                return new TestMessageModel();
            }

            public void AddRecord(TestMessageModel record)
            {
                throw new InvalidOperationException();
            }

            public void UpdateRecord(TestMessageModel record)
            {
                throw new InvalidOperationException();
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var record in domainModel.GetSiteCollection<TestMail>().Where(m => recordIds.Contains(m.Id)))
                    {
                        domainModel.GetSiteCollection<TestMail>().Remove(record);
                    }

                    domainModel.SaveChanges();
                    return true;
                }
            }

            public bool DeleteOnly { get { return true; } }
        }

        public long TestMailId { get; set; }

        [InputField(InputFieldType.Email, MainModuleTextIds.FromEmail, ReadOnly = true)]
        [Column(MainModuleTextIds.FromEmail)]
        public string From { get; set; }

        [InputField(InputFieldType.Email, MainModuleTextIds.ToEmail, ReadOnly = true)]
        [Column(MainModuleTextIds.ToEmail)]
        public string To { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.Subject, ReadOnly = true)]
        [Column(MainModuleTextIds.Subject)]
        public string Subject { get; set; }

        [InputField(InputFieldType.MultiLineText, MainModuleTextIds.Body, ReadOnly = true)]
        public string Body { get; set; }

        [InputField(InputFieldType.Date, MainModuleTextIds.DateSent, ReadOnly = true)]
        [Column(MainModuleTextIds.DateSent)]
        public DateTime Sent { get; set; }

        [InputField(InputFieldType.Text, MainModuleTextIds.Template, ReadOnly = true)]
        [Column(MainModuleTextIds.Template)]
        public string Template { get; set; }
    }
}
