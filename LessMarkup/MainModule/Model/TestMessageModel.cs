/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Common;
using LessMarkup.Engine.Language;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;

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

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter)
            {
                return domainModel.GetSiteCollection<TestMail>().OrderByDescending(e => e.Sent).Select(e => e.TestMailId);
            }

            public IQueryable<TestMessageModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return domainModel.GetSiteCollection<TestMail>().Where(e => ids.Contains(e.TestMailId)).Select(e => new TestMessageModel
                {
                    Body = e.Body,
                    From = e.From,
                    Sent = e.Sent,
                    Subject = e.Subject,
                    Template = e.Template,
                    To = e.To,
                    TestMailId = e.TestMailId
                });
            }

            public bool Filtered { get { return false; } }

            public TestMessageModel AddRecord(TestMessageModel record, bool returnObject)
            {
                throw new InvalidOperationException();
            }

            public TestMessageModel UpdateRecord(TestMessageModel record, bool returnObject)
            {
                throw new InvalidOperationException();
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var record in domainModel.GetSiteCollection<TestMail>().Where(m => recordIds.Contains(m.TestMailId)))
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
