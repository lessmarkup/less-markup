/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.Forum.Model
{
    public class UserPropertiesProvider : IUserPropertyProvider
    {
        private readonly IDataCache _dataCache;

        public UserPropertiesProvider(IDataCache dataCache)
        {
            _dataCache = dataCache;
        }

        public IEnumerable<UserProperty> GetProperties(long userId)
        {
            var cache = _dataCache.Get<PostStatisticsCache>();
            cache.ReadUsers(new [] { userId });
            var user = cache.Get(userId);

            return new List<UserProperty>
            {
                new UserProperty
                {
                    Name = LanguageHelper.GetText(Constants.ModuleType.Forum, ForumTextIds.Posts),
                    Type = UserPropertyType.Text,
                    Value = user.Posts
                }
            };
        }
    }
}
