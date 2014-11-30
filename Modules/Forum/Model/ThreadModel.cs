/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Forum.Model
{
    [RecordModel(CollectionType = typeof(Collection), TitleTextId = ForumTextIds.Threads, DataType = typeof(Thread))]
    public class ThreadModel
    {
        public class WhoViews
        {
            public long UserId { get; set; }
            public string Name { get; set; }
            public string ProfileUrl { get; set; }
        }

        public class Collection : IEditableModelCollection<ThreadModel>
        {
            private long _forumId;
            private NodeAccessType _accessType;
            private readonly IDomainModelProvider _domainModelProvider;
            private readonly IChangeTracker _changeTracker;
            private readonly ICurrentUser _currentUser;

            public Collection(ICurrentUser currentUser, IDomainModelProvider domainModelProvider, IChangeTracker changeTracker)
            {
                _currentUser = currentUser;
                _domainModelProvider = domainModelProvider;
                _changeTracker = changeTracker;
            }

            public IReadOnlyCollection<long> ReadIds(IQueryBuilder query, bool ignoreOrder)
            {
                query = query.From<Thread>().Where("ForumId = $", _forumId);

                if (_accessType != NodeAccessType.Manage)
                {
                    query = query.Where("Removed = 0");
                }

                if (!ignoreOrder)
                {
                    query = query.OrderByDescending("Updated");
                }

                return query.ToIdList();
            }

            public IReadOnlyCollection<ThreadModel> Read(IQueryBuilder query, List<long> ids)
            {
                if (ids.Count == 0)
                {
                    return new List<ThreadModel>();
                }

                var userId = _currentUser.UserId;

                var idsText = string.Join(",", ids);

                var queryText = string.Format(
                    "SELECT t.Id ThreadId, t.Name, t.Description, t.Created, t.Updated, t.Path, t.Removed, t.Closed, a.Name Author, " +
                    "t.AuthorId, lu.Name LastUser, p.UserId LastUserId, p.Created LastCreated, ts.PostCount Posts, CASE WHEN tv.Unread IS NULL AND $0 IS NOT NULL THEN ts.PostCount ELSE tv.Unread END Unread, v.Views " +
                    "FROM (SELECT * FROM Threads WHERE Id IN ({0})) t " +
                    "LEFT JOIN (SELECT ThreadId, COUNT(Id) PostCount, MAX(Created) LastCreated FROM Posts WHERE ThreadId IN ({0}) GROUP BY ThreadId) ts ON t.Id = ts.ThreadId " +
                    "LEFT JOIN Posts p ON p.ThreadId = t.Id AND p.Created = ts.LastCreated " +
                    "LEFT JOIN (SELECT tv1.ThreadId, COUNT(p1.Id) Unread FROM (SELECT ThreadId, MAX(Updated) Updated FROM ThreadViews WHERE ThreadId IN ({0}) AND $0 IS NOT NULL AND UserId = $0 GROUP BY ThreadId) tv1 LEFT JOIN Posts p1 ON $0 IS NOT NULL AND p1.ThreadId = tv1.ThreadId AND p1.Created > tv1.Updated WHERE tv1.Updated IS NOT NULL GROUP BY tv1.ThreadId) tv ON tv.ThreadId = t.Id " +
                    "LEFT JOIN Users a on a.Id = t.AuthorId " +
                    "LEFT JOIN Users lu ON lu.Id = p.UserId " +
                    "LEFT JOIN (SELECT ThreadId, COUNT(Id) Views FROM ThreadViews WHERE ThreadId IN ({0}) GROUP BY ThreadId) v ON v.ThreadId = t.Id", idsText);

                if (_accessType != NodeAccessType.Manage)
                {
                    queryText += " WHERE t.Removed = 0 AND ts.PostCount > 0";
                }

                return query.Execute<ThreadModel>(queryText, userId);
            }

            public void Initialize(long? objectId, NodeAccessType accessType)
            {
                if (!objectId.HasValue)
                {
                    throw new NullReferenceException("objectId");
                }

                _forumId = objectId.Value;
                _accessType = accessType;
            }

            public int CollectionId { get { return DataHelper.GetCollectionId<Thread>(); } }

            public ThreadModel CreateRecord()
            {
                return new ThreadModel();
            }

            public void AddRecord(ThreadModel record)
            {
                throw new UnauthorizedAccessException();
            }

            public void UpdateRecord(ThreadModel record)
            {
                if (_accessType != NodeAccessType.Manage)
                {
                    throw new UnauthorizedAccessException();
                }

                using (var domainModel = _domainModelProvider.Create())
                {
                    var thread = domainModel.Query().From<Thread>().Where("Id = $ AND ForumId = $", record.ThreadId, _forumId).First<Thread>();

                    thread.Description = record.Description;

                    if (thread.Name != record.Name)
                    {
                        thread.Name = record.Name;

                        var generatedPath = TextToUrl.Generate(thread.Name);

                        var paths = new HashSet<string>(domainModel.Query().From<Thread>().Where("Id != $ AND ForumId = $", record.ThreadId, _forumId).ToList<Thread>("Path").Select(t => t.Path));

                        var index = 1;

                        string path;

                        for (; ; index++)
                        {
                            path = index == 1 ? generatedPath : string.Format("{0}-{1}", generatedPath, index);

                            if (!paths.Contains(path))
                            {
                                break;
                            }
                        }

                        thread.Path = path;
                    }

                    domainModel.Update(thread);
                    _changeTracker.AddChange(thread, EntityChangeType.Updated, domainModel);

                    record.Path = thread.Path;
                }
            }

            public bool DeleteRecords(IEnumerable<long> recordIds)
            {
                throw new UnauthorizedAccessException();
            }

            public bool DeleteOnly { get { return false; } }
        }

        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;

        ThreadModel()
        {
        }

        public ThreadModel(IDomainModelProvider domainModelProvider, IChangeTracker changeTracker)
        {
            _domainModelProvider = domainModelProvider;
            _changeTracker = changeTracker;
        }

        public static ThreadModel GetByPath(long forumId, string path, IDomainModelProvider domainModelProvider)
        {
            using (var domainModel = domainModelProvider.Create())
            {
                var thread = domainModel.Query().From<Thread>().Where("ForumId = $ AND Path = $", forumId, path).FirstOrDefault<Thread>();

                if (thread == null)
                {
                    return null;
                }

                return new ThreadModel
                {
                    ThreadId = thread.Id,
                    Path = thread.Path,
                    Name = thread.Name,
                    Description = thread.Description
                };
            }
        }

        public long ThreadId { get; set; }

        [Column(ForumTextIds.Name, CellTemplate = "~/Views/ThreadNameCell.html", CellClass = "forum-cell")]
        [RecordSearch]
        [InputField(InputFieldType.Text, ForumTextIds.ThreadName, Required = true)]
        public string Name { get; set; }

        [RecordSearch]
        [InputField(InputFieldType.Text, ForumTextIds.Description)]
        public string Description { get; set; }

        public string Path { get; set; }

        public bool Removed { get; set; }

        public bool Closed { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public string Author { get; set; }

        public long? AuthorId { get; set; }

        public string AuthorUrl { get; set; }

        public string LastUser { get; set; }

        public long? LastUserId { get; set; }

        public string LastUserUrl { get; set; }

        public DateTime? LastCreated { get; set; }

        public List<WhoViews> ActiveUsers { get; set; }

        public string UnreadUrl { get; set; }

        public int Unread { get; set; }

        [Column(ForumTextIds.Views, Width = "50")]
        public int Views { get; set; }

        [Column(ForumTextIds.Posts, CellTemplate = "~/Views/ThreadPostsCell.html", Width = "40%", CellClass = "forum-cell")]
        public int Posts { get; set; }

        public object Delete(NodeAccessType accessType, long forumId, long threadId, bool isManager)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var thread = domainModel.Query().From<Thread>().Where("Id = $ AND ForumId = $", threadId, forumId).First<Thread>();
                thread.Removed = true;
                _changeTracker.AddChange<Thread>(threadId, EntityChangeType.Removed, domainModel);
                domainModel.Update(thread);

                _changeTracker.Invalidate();

                if (!isManager)
                {
                    return new {removed = true};
                }

                var collection = DependencyResolver.Resolve<Collection>();
                collection.Initialize(forumId, accessType);

                var model = collection.Read(domainModel.Query(), new List<long> {threadId}).First();

                return new {record = model};
            }
        }

        public object Restore(NodeAccessType accessType, long forumId, long threadId, bool isManager)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var thread = domainModel.Query().Find<Thread>(threadId);
                thread.Removed = false;
                domainModel.Update(thread);
                _changeTracker.AddChange<Thread>(threadId, EntityChangeType.Added, domainModel);

                _changeTracker.Invalidate();

                var collection = DependencyResolver.Resolve<Collection>();
                collection.Initialize(forumId, accessType);

                var model = collection.Read(domainModel.Query(), new List<long> { threadId }).First();

                return new { record = model };
            }
        }

        public object Close(NodeAccessType accessType, long forumId, long threadId, bool isManager)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var thread = domainModel.Query().Find<Thread>(threadId);
                thread.Closed = true;
                domainModel.Update(thread);
                _changeTracker.AddChange<Thread>(threadId, EntityChangeType.Added, domainModel);

                var collection = DependencyResolver.Resolve<Collection>();
                collection.Initialize(forumId, accessType);

                var model = collection.Read(domainModel.Query(), new List<long> { threadId }).First();

                return new { record = model };
            }
        }

        public object Open(NodeAccessType accessType, long forumId, long threadId, bool isManager)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var thread = domainModel.Query().Find<Thread>(threadId);
                thread.Closed = false;
                domainModel.Update(thread);
                _changeTracker.AddChange<Thread>(threadId, EntityChangeType.Added, domainModel);

                var collection = DependencyResolver.Resolve<Collection>();
                collection.Initialize(forumId, accessType);

                var model = collection.Read(domainModel.Query(), new List<long> { threadId }).First();

                return new { record = model };
            }
        }

        public void PostProcess(string fullPath, IDataCache dataCache)
        {
            if (AuthorId.HasValue)
            {
                AuthorUrl = UserHelper.GetUserProfileLink(AuthorId.Value);
            }

            if (LastUserId.HasValue)
            {
                LastUserUrl = UserHelper.GetUserProfileLink(LastUserId.Value);
            }

            if (Unread > 0)
            {
                var siteConfiguration = dataCache.Get<ISiteConfiguration>();
                var recordsPerPage = siteConfiguration.RecordsPerPage;
                if (recordsPerPage > 0)
                {
                    UnreadUrl = RecordListHelper.PageLink("", ((Posts - Unread)/recordsPerPage) + 1);
                }
            }
        }
    }
}
