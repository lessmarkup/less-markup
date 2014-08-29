/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public class RecordListLinkNodeHandler<T> : RecordListNodeHandler<T> where T : class
    {
        struct CellLinkHandler
        {
            public object Text;
            public Type HandlerType;
        }

        private readonly Dictionary<string, CellLinkHandler> _cellLinkHandlers = new Dictionary<string, CellLinkHandler>();

        public RecordListLinkNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache, ICurrentUser currentUser)
            : base(domainModelProvider, dataCache, currentUser)
        {
        }

        protected void AddCellLink<TH>(object text, string link) where TH : INodeHandler
        {
            _cellLinkHandlers.Add(link, new CellLinkHandler { Text = text, HandlerType = typeof(TH) });
            AddRecordLink(text, "{" + IdProperty.Name + "}/" + link);
        }

        protected override bool HasChildren
        {
            get { return _cellLinkHandlers.Any(); }
        }

        protected override ChildHandlerSettings GetChildHandler(string path)
        {
            var split = path.Split(new[] { '/' });
            if (split.Length < 2)
            {
                return null;
            }

            long recordId;
            if (!long.TryParse(split[0], out recordId))
            {
                return null;
            }

            CellLinkHandler cellLinkHandler;
            if (!_cellLinkHandlers.TryGetValue(split[1], out cellLinkHandler))
            {
                return null;
            }

            var handler = (INodeHandler) DependencyResolver.Resolve(cellLinkHandler.HandlerType);

            if (handler == null)
            {
                return null;
            }

            var localPath = string.Join("/", split.Take(2));

            handler.Initialize(recordId, null, null, split[0], FullPath + "/" + localPath, AccessType);

            return new ChildHandlerSettings
            {
                Path = localPath,
                Title = LanguageHelper.GetText(RecordModel.ModuleType, cellLinkHandler.Text),
                Handler = handler,
                Id = recordId,
                Rest = string.Join("/", split.Skip(2))
            };
        }
    }
}
