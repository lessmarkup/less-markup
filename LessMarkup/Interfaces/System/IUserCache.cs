/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.Interfaces.System
{
    public interface IUserCache : ICacheHandler
    {
        string Name { get; }
        bool IsRemoved { get; }
        bool IsAdministrator { get; }
        bool IsApproved { get;  }
        bool EmailConfirmed { get; }
        IReadOnlyList<long> Groups { get; }
        string Email { get; }
        string Title { get; }
        bool IsBlocked { get; }
        DateTime? UnblockTime { get; }
        string Properties { get; }
        long? AvatarImageId { get; set; }
        long? UserImageId { get; set; }
        IReadOnlyList<Tuple<ICachedNodeInformation, NodeAccessType>> Nodes { get; }
    }
}
