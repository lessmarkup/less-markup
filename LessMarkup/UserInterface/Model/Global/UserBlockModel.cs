/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.Global
{
    [RecordModel(TitleTextId = UserInterfaceTextIds.BlockUser)]
    public class UserBlockModel
    {
        [InputField(InputFieldType.Text, UserInterfaceTextIds.BlockReason)]
        public string Reason { get; set; }

        public string InternalReason { get; set; }

        [InputField(InputFieldType.Date, UserInterfaceTextIds.UnblockTime)]
        public DateTime? UnblockTime { get; set; }

        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IChangeTracker _changeTracker;
        private readonly ICurrentUser _currentUser;
        private readonly ISiteMapper _siteMapper;

        public UserBlockModel(IDomainModelProvider domainModelProvider, IChangeTracker changeTracker, ICurrentUser currentUser, ISiteMapper siteMapper)
        {
            _domainModelProvider = domainModelProvider;
            _changeTracker = changeTracker;
            _currentUser = currentUser;
            _siteMapper = siteMapper;
        }

        public void BlockUser(long? siteId, long userId)
        {
            if (!siteId.HasValue)
            {
                siteId = _siteMapper.SiteId;
                if (!siteId.HasValue)
                {
                    throw new UnauthorizedAccessException();
                }
            }

            var currentUserId = _currentUser.UserId;

            if (!currentUserId.HasValue)
            {
                throw new UnauthorizedAccessException();
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var user = domainModel.GetCollection<DataObjects.Security.User>().Single(u => u.Id == userId && u.SiteId == siteId.Value);
                user.IsBlocked = true;
                user.BlockReason = Reason;
                if (UnblockTime.HasValue && UnblockTime.Value < DateTime.UtcNow)
                {
                    UnblockTime = null;
                }
                user.UnblockTime = UnblockTime;
                user.LastBlock = DateTime.UtcNow;

                var blockHistory = new UserBlockHistory
                {
                    BlockedByUserId = currentUserId.Value,
                    BlockedToTime = UnblockTime,
                    Reason = Reason,
                    UserId = userId,
                    Created = user.LastBlock.Value,
                    InternalReason = InternalReason,
                };

                domainModel.GetCollection<UserBlockHistory>().Add(blockHistory);

                _changeTracker.AddChange(user, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();
            }
        }

        public void UnblockUser(long? siteId, long userId)
        {
            if (!siteId.HasValue)
            {
                siteId = _siteMapper.SiteId;
                if (!siteId.HasValue)
                {
                    throw new UnauthorizedAccessException();
                }
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var user = domainModel.GetCollection<DataObjects.Security.User>().Single(u => u.Id == userId && u.SiteId == siteId.Value);

                if (!user.IsBlocked)
                {
                    return;
                }

                user.IsBlocked = false;

                foreach (var history in domainModel.GetCollection<UserBlockHistory>().Where(h => h.UserId == userId && !h.IsUnblocked))
                {
                    history.IsUnblocked = true;
                }

                _changeTracker.AddChange(user, EntityChangeType.Updated, domainModel);
                domainModel.SaveChanges();
            }
        }
    }
}
