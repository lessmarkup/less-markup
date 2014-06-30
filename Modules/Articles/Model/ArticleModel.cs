/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Articles.DataObjects;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;

namespace LessMarkup.Articles.Model
{
    [RecordModel(TitleTextId = ArticlesTextIds.EditArticle, CollectionType = typeof(ModelManager), EntityType = EntityType.Article)]
    public class ArticleModel
    {
        public class ModelManager : IEditableModelCollection<ArticleModel>
        {
            private readonly ICurrentUser _currentUser;
            private readonly IDomainModelProvider _domainModelProvider;
            private readonly IChangeTracker _changeTracker;
            private readonly IHtmlSanitizer _htmlSanitizer;

            public ModelManager(ICurrentUser currentUser, IDomainModelProvider domainModelProvider, IChangeTracker changeTracker, IHtmlSanitizer htmlSanitizer)
            {
                _currentUser = currentUser;
                _domainModelProvider = domainModelProvider;
                _changeTracker = changeTracker;
                _htmlSanitizer = htmlSanitizer;
            }

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter)
            {
                return domainModel.GetSiteCollection<Article>().Select(a => a.ArticleId);
            }

            public IQueryable<ArticleModel> Read(IDomainModel domainModel, List<long> ids)
            {
                return domainModel.GetSiteCollection<Article>().Where(a => ids.Contains(a.ArticleId))
                    .Select(a => new ArticleModel
                    {
                        ArticleId = a.ArticleId,
                        Body = a.Body,
                        Created = a.Created,
                        Updated = a.Updated,
                        Order = a.Order,
                        MenuId = a.MenuId,
                    });
            }

            public bool Filtered { get { return false; } }

            public ArticleModel AddRecord(ArticleModel record, bool returnObject)
            {
                var currentUserId = _currentUser.UserId;
                if (!currentUserId.HasValue)
                {
                    throw new AccessViolationException();
                }

                using (var domainModel = _domainModelProvider.Create())
                {
                    var article = domainModel.GetSiteCollection<Article>().Create();
                    article.Created = DateTime.UtcNow;
                    article.AuthorId = currentUserId.Value;
                    article.MenuId = record.MenuId;
                    article.Body = _htmlSanitizer.Sanitize(record.Body);

                    domainModel.GetSiteCollection<Article>().Add(article);
                    domainModel.SaveChanges();

                    _changeTracker.AddChange(article.ArticleId, EntityType.Article, EntityChangeType.Added, domainModel);
                    domainModel.SaveChanges();

                    record.ArticleId = article.ArticleId;
                }

                return record;
            }

            public ArticleModel UpdateRecord(ArticleModel record, bool returnObject)
            {
                var currentUserId = _currentUser.UserId;
                if (!currentUserId.HasValue)
                {
                    throw new UnauthorizedAccessException();
                }

                using (var domainModel = _domainModelProvider.CreateWithTransaction())
                {
                    var article = domainModel.GetSiteCollection<Article>().Single(a => a.ArticleId == record.ArticleId);

                    article.Updated = DateTime.UtcNow;
                    article.MenuId = record.MenuId;
                    article.Order = record.Order;
                    article.Body = _htmlSanitizer.Sanitize(record.Body);

                    _changeTracker.AddChange(article.ArticleId, EntityType.Article, EntityChangeType.Updated, domainModel);

                    domainModel.SaveChanges();

                    domainModel.CompleteTransaction();
                }

                return record;
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var articleId in recordIds)
                    {
                        var article = domainModel.GetSiteCollection<Article>().FirstOrDefault(a => a.ArticleId == articleId);
                        if (article == null)
                        {
                            continue;
                        }
                        domainModel.GetSiteCollection<Article>().Remove(article);
                        _changeTracker.AddChange(article.ArticleId, EntityType.Article, EntityChangeType.Removed, domainModel);
                    }
                    domainModel.SaveChanges();
                }

                return true;
            }
        }

        public long? ArticleId { get; set; }
        [InputField(InputFieldType.RichText, ArticlesTextIds.Body, Required = true)]
        public string Body { get; set; }
        [InputField(InputFieldType.Text, ArticlesTextIds.MenuId, Required = true)]
        public string MenuId { get; set; }
        [InputField(InputFieldType.Number, ArticlesTextIds.Order)]
        public int? Order { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
    }
}
