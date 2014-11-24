/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Linq;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.Model.User;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.User
{
    public class UserCardsNodeHandler : RecordListNodeHandler<UserCardModel>
    {
        private readonly IDataCache _dataCache;

        public UserCardsNodeHandler(ILightDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
            _dataCache = dataCache;
        }

        protected override bool HasChildren
        {
            get { return true; }
        }

        protected override ChildHandlerSettings GetChildHandler(string path)
        {
            var parts = path.Split(new[] {'/'});
            if (parts.Length == 0)
            {
                return null;
            }

            long userId;
            if (!long.TryParse(parts[0], out userId))
            {
                return null;
            }

            var handler = DependencyResolver.Resolve<UserCardNodeHandler>();

            handler.Initialize(userId);

            ((INodeHandler)handler).Initialize(null, null, null, parts[0], "", AccessType);

            var userCache = _dataCache.Get<IUserCache>(userId);

            return new ChildHandlerSettings
            {
                Handler = handler,
                Id = userId,
                Path = parts[0],
                Rest = string.Join("/", parts.Skip(1)),
                Title = userCache.Name
            };
        }
    }
}
