/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Framework.Helpers;
using LessMarkup.Framework.Language;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.UserInterface.PageHandlers;
using LessMarkup.UserInterface.PageHandlers.Configuration;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class PageCache : ICacheHandler
    {
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IModuleIntegration _moduleIntegration;

        private readonly List<CachedPageInformation> _cachedPages = new List<CachedPageInformation>();
        private readonly Dictionary<long, CachedPageInformation> _idToPage = new Dictionary<long, CachedPageInformation>();
        private CachedPageInformation _rootPage;

        public PageCache(IDomainModelProvider domainModelProvider, IModuleIntegration moduleIntegration)
        {
            _domainModelProvider = domainModelProvider;
            _moduleIntegration = moduleIntegration;
        }

        public static string GetViewPath(string viewName)
        {
            if (viewName.EndsWith("PageHandler"))
            {
                viewName = viewName.Substring(0, viewName.Length - "PageHandler".Length);
            }

            return "~/Views/Structure/" + viewName + ".cshtml";
        }

        private void InitializeTree(CachedPageInformation node)
        {
            if (!string.IsNullOrWhiteSpace(node.HandlerId))
            {
                var handler = _moduleIntegration.GetPageHandler(node.HandlerId);

                if (handler != null)
                {
                    node.HandlerType = handler.Item1;
                    node.HandlerModuleType = handler.Item2;
                }
            }

            node.Root = _rootPage;

            if (node.Parent == null)
            {
                // root
                node.Path = "";
                node.FullPath = "";
            }
            else
            {
                node.Path = node.Path.Trim().ToLower();

                if (string.IsNullOrEmpty(node.Path))
                {
                    return;
                }

                node.FullPath = string.IsNullOrEmpty(node.Parent.FullPath) ? node.Path : node.Parent.FullPath + "/" + node.Path;
            }

            _cachedPages.Add(node);
            _idToPage[node.PageId] = _rootPage;

            foreach (var child in node.Children.Where(c => c.Enabled))
            {
                child.Parent = node;
                InitializeTree(child);
            }
        }

        private void InitializeNode(CachedPageInformation node, List<CachedPageInformation> pages, int from, int count)
        {
            if (count == 0)
            {
                return;
            }

            var lowLevel = pages[from].Level;
            var to = from + count;

            node.Children = new List<CachedPageInformation>();

            var firstPage = pages[from];
            node.Children.Add(firstPage);

            from++;

            for (int i = from; i < to;)
            {
                var nextPage = pages[i];
                if (nextPage.Level <= lowLevel)
                {
                    node.Children.Add(nextPage);
                    i++;
                    continue;
                }
                var parent = node.Children.Last();
                var childFrom = i;
                for (i++; i < to; i++)
                {
                    if (pages[i].Level <= lowLevel)
                    {
                        break;
                    }
                }

                InitializeNode(parent, pages, childFrom, i - childFrom);
            }
        }

        public CachedPageInformation GetPage(long pageId)
        {
            CachedPageInformation ret;
            return _idToPage.TryGetValue(pageId, out ret) ? ret : null;
        }

        public void GetPage(string path, out CachedPageInformation page, out string rest)
        {
            var pathParts = (path ?? "").ToLower().Split(new[] {'/'}).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            if (pathParts.Count == 0)
            {
                page = _rootPage;
                rest = "";
                return;
            }

            page = _rootPage;

            while (pathParts.Count > 0)
            {
                var pathPart = pathParts[0].Trim();
                if (string.IsNullOrEmpty(pathPart))
                {
                    pathParts.RemoveAt(0);
                    continue;
                }

                var child = page.Children == null ? null : page.Children.FirstOrDefault(c => c.Path == pathPart);

                if (child == null)
                {
                    break;
                }

                page = child;

                pathParts.RemoveAt(0);
            }

            rest = pathParts.Count > 0 ? string.Join("/", pathParts) : "";
        }

        public void Initialize(out DateTime? expirationTime, long? objectId = null)
        {
            if (objectId.HasValue)
            {
                throw new ArgumentOutOfRangeException("objectId");
            }

            expirationTime = null;

            List<CachedPageInformation> cachedPages;

            using (var domainModel = _domainModelProvider.Create())
            {
                cachedPages = domainModel.GetSiteCollection<Page>().OrderBy(p => p.Order).Select(p => new CachedPageInformation
                {
                    PageId = p.PageId,
                    Enabled = p.Enabled,
                    HandlerId = p.HandlerId,
                    Level = p.Level,
                    Order = p.Order,
                    Path = p.Path,
                    Title = p.Title,
                    Settings = p.Settings,
                    AccessList = p.PageAccess.Select(a => new CachedPageAccess
                    {
                        AccessType = a.AccessType,
                        GroupId = a.GroupId,
                        UserId = a.UserId
                    }).ToList()
                }).ToList();
            }

            if (cachedPages.Count == 0)
            {
                cachedPages.Add(new CachedPageInformation
                {
                    AccessList = new List<CachedPageAccess> {new CachedPageAccess {AccessType = PageAccessType.Read}},
                    HandlerModuleType = ModuleType.Core,
                    HandlerType = typeof (EmptyRootPageHandler),
                    Title = "Home",
                    PageId = 1,
                    HandlerId = "home",
                    Children = new List<CachedPageInformation>(),
                });
            }

            _rootPage = cachedPages[0];

            InitializeNode(_rootPage, cachedPages, 1, cachedPages.Count-1);

            InitializeTree(_rootPage);

            _rootPage.Root = _rootPage;

            var pageId = _idToPage.Keys.Max() + 1;

            var configurationPage = new CachedPageInformation
            {
                AccessList = new List<CachedPageAccess>
                {
                    new CachedPageAccess {AccessType = PageAccessType.NoAccess},
                },
                FullPath = "configuration",
                Path = "configuration",
                HandlerModuleType = ModuleType.Core,
                ParentPageId = _rootPage.PageId,
                Parent = _rootPage,
                Title = LanguageHelper.GetText(ModuleType.Core, CoreTextIds.Configuration),
                HandlerType = typeof (ConfigurationRootPageHandler),
                PageId = pageId,
                HandlerId = "configuration",
                Root = _rootPage
            };

            _rootPage.Children.Add(configurationPage);
            _idToPage[pageId] = configurationPage;

            _cachedPages.Clear();

        }

        public bool Expires(EntityType entityType, long entityId, EntityChangeType changeType)
        {
            return entityType == EntityType.Page;
        }

        private static readonly EntityType[] _handledTypes = {EntityType.Page};

        public EntityType[] HandledTypes { get { return _handledTypes; } }
    }
}
