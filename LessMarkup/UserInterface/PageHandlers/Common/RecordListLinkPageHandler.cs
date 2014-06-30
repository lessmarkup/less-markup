/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.UserInterface.PageHandlers.Common
{
    public abstract class RecordListLinkPageHandler<T> : RecordListPageHandler<T> where T : class
    {
        struct CellLinkHandler
        {
            public string Text;
            public Type HandlerType;
        }

        private readonly Dictionary<string, CellLinkHandler> _cellLinkHandlers = new Dictionary<string, CellLinkHandler>();

        protected RecordListLinkPageHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
        }

        protected void AddCellLink<TH>(string text, string link) where TH : IRecordPageHandler
        {
            _cellLinkHandlers.Add(link, new CellLinkHandler { Text = text, HandlerType = typeof(TH) });
            AddCellLink(text, link);
        }

        public override bool HasChildren
        {
            get { return _cellLinkHandlers.Any(); }
        }

        public override ChildHandlerSettings GetChildHandler(string path)
        {
            var split = path.Split(new[] {'/'});
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

            var handler = (IRecordPageHandler) DependencyResolver.Resolve(cellLinkHandler.HandlerType);

            if (handler == null)
            {
                return null;
            }

            handler.Initialize(recordId);

            return new ChildHandlerSettings
            {
                Path = string.Join("/", split.Take(2)),
                Title = cellLinkHandler.Text,
                Handler = handler,
                Id = recordId,
                Rest = string.Join("/", split.Skip(2))
            };
        }
    }
}
