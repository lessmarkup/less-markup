/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Common;
using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.MainModule.Model
{
    [RecordModel(CollectionType = typeof(Collection), TitleTextId = MainModuleTextIds.ViewTestMessage, DataType = typeof(TestMail))]
    public class TestMessageModel
    {
        public class Collection : IEditableModelCollection<TestMessageModel>
        {
            private readonly ILightDomainModelProvider _domainModelProvider;

            public Collection(ILightDomainModelProvider domainModelProvider)
            {
                _domainModelProvider = domainModelProvider;
            }

            public IReadOnlyCollection<long> ReadIds(ILightQueryBuilder query, bool ignoreOrder)
            {
                if (!ignoreOrder)
                {
                    query = query.OrderByDescending("Sent");
                }

                return query.ToIdList();
            }

            public int CollectionId { get { return DataHelper.GetCollectionId<TestMail>(); } }

            public IReadOnlyCollection<TestMessageModel> Read(ILightQueryBuilder queryBuilder, List<long> ids)
            {
                return queryBuilder.WhereIds(ids).ToList<TestMail>().Select(e => new TestMessageModel
                {
                    Body = e.Body,
                    From = e.From,
                    Sent = e.Sent,
                    Subject = e.Subject,
                    Template = e.Template,
                    To = e.To,
                    TestMailId = e.Id
                }).ToList();
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
                    foreach (var id in recordIds)
                    {
                        domainModel.Delete<TestMail>(id);
                    }

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
