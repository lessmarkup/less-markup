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
    [RecordModel(CollectionType = typeof(Collection), TitleTextId = ForumTextIds.Threads)]
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

            public IQueryable<long> ReadIds(IDomainModel domainModel, string filter, bool ignoreOrder)
            {
                var query = domainModel.GetSiteCollection<Thread>().Where(t => t.ForumId == _forumId);

                if (_accessType != NodeAccessType.Manage)
                {
                    query = query.Where(t => !t.Removed && t.Posts.Any(p => !p.Removed));
                }

                if (!ignoreOrder)
                {
                    query = query.OrderByDescending(t => t.Updated);
                }

                query = RecordListHelper.GetFilterAndOrderQuery(query, filter, typeof (ThreadModel));

                return query.Select(t => t.Id);
            }

            public IQueryable<ThreadModel> Read(IDomainModel domainModel, List<long> ids)
            {
                var userId = _currentUser.UserId;

                var query = domainModel.GetSiteCollection<Thread>()
                    .Where(t => t.ForumId == _forumId && ids.Contains(t.Id))
                    .Select(t => new
                    {
                        Thread = t,
                        Posts = t.Posts.Where(p => !p.Removed),
                        LastUpdate = userId.HasValue
                                ? t.Views.Where(v => v.UserId == userId).Max(v => v.Updated)
                                : (DateTime?) null
                    }).Select(t => new
                    {
                        t.Thread,
                        t.LastUpdate,
                        Last = t.Posts.OrderByDescending(p => p.Created).FirstOrDefault(),
                        PostCount = t.Posts.Count(),
                        t.Posts
                    }).Select(t => new
                    {
                        t.Thread,
                        t.Last,
                        t.PostCount,
                        Unread = userId.HasValue ? (t.LastUpdate.HasValue ? t.Posts.Count(p => p.Created > t.LastUpdate) : t.PostCount) : 0
                    });

                if (_accessType != NodeAccessType.Manage)
                {
                    query = query.Where(t => !t.Thread.Removed && t.PostCount > 0);
                }

                return query
                    .Select(t => new ThreadModel
                        {
                            ThreadId = t.Thread.Id,
                            Name = t.Thread.Name,
                            Description = t.Thread.Description,
                            Created = t.Thread.Created,
                            Updated = t.Thread.Updated,
                            Path = t.Thread.Path,
                            Removed = t.Thread.Removed,
                            Closed = t.Thread.Closed,
                            Author = t.Thread.Author.Name,
                            AuthorId = t.Thread.AuthorId,
                            LastUser = t.Last.User.Name,
                            LastUserId = t.Last.UserId,
                            LastCreated = t.Last.Created,
                            Posts = t.PostCount,
                            Unread = t.Unread,
                            Views = t.Thread.Views.Sum(v => (int?)v.Views) ?? 0
                        });
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

            public int CollectionId { get { return DataHelper.GetCollectionIdVerified<Thread>(); } }

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
                    var thread = domainModel.GetSiteCollection<Thread>().Single(t => t.Id == record.ThreadId && t.ForumId == _forumId);

                    thread.Description = record.Description;

                    if (thread.Name != record.Name)
                    {
                        thread.Name = record.Name;

                        var generatedPath = TextToUrl.Generate(thread.Name);

                        var paths =
                            new HashSet<string>(
                                domainModel.GetSiteCollection<Thread>()
                                    .Where(t => t.Id != record.ThreadId && t.ForumId == _forumId)
                                    .Select(t => t.Path));

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

                    _changeTracker.AddChange(thread, EntityChangeType.Updated, domainModel);
                    domainModel.SaveChanges();

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
                var thread = domainModel.GetSiteCollection<Thread>().FirstOrDefault(t => t.ForumId == forumId && t.Path == path);
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
                var thread = domainModel.GetSiteCollection<Thread>().First(t => t.Id == threadId && t.ForumId == forumId);
                thread.Removed = true;
                _changeTracker.AddChange<Thread>(threadId, EntityChangeType.Removed, domainModel);
                domainModel.SaveChanges();

                _changeTracker.Invalidate();

                if (!isManager)
                {
                    return new {removed = true};
                }

                var collection = DependencyResolver.Resolve<Collection>();
                collection.Initialize(forumId, accessType);

                var model = collection.Read(domainModel, new List<long> {threadId}).First();

                return new {record = model};
            }
        }

        public object Restore(NodeAccessType accessType, long forumId, long threadId, bool isManager)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var thread = domainModel.GetSiteCollection<Thread>().First(t => t.Id == threadId);
                thread.Removed = false;
                _changeTracker.AddChange<Thread>(threadId, EntityChangeType.Added, domainModel);
                domainModel.SaveChanges();

                _changeTracker.Invalidate();

                var collection = DependencyResolver.Resolve<Collection>();
                collection.Initialize(forumId, accessType);

                var model = collection.Read(domainModel, new List<long> { threadId }).First();

                return new { record = model };
            }
        }

        public object Close(NodeAccessType accessType, long forumId, long threadId, bool isManager)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var thread = domainModel.GetSiteCollection<Thread>().First(t => t.Id == threadId);
                thread.Closed = true;
                _changeTracker.AddChange<Thread>(threadId, EntityChangeType.Added, domainModel);
                domainModel.SaveChanges();

                var collection = DependencyResolver.Resolve<Collection>();
                collection.Initialize(forumId, accessType);

                var model = collection.Read(domainModel, new List<long> { threadId }).First();

                return new { record = model };
            }
        }

        public object Open(NodeAccessType accessType, long forumId, long threadId, bool isManager)
        {
            using (var domainModel = _domainModelProvider.Create())
            {
                var thread = domainModel.GetSiteCollection<Thread>().First(t => t.Id == threadId);
                thread.Closed = false;
                _changeTracker.AddChange<Thread>(threadId, EntityChangeType.Added, domainModel);
                domainModel.SaveChanges();

                var collection = DependencyResolver.Resolve<Collection>();
                collection.Initialize(forumId, accessType);

                var model = collection.Read(domainModel, new List<long> { threadId }).First();

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
