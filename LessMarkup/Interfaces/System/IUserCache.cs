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
        bool IsApproved { get; set; }
        bool IsGlobalAdministrator { get; }
        IReadOnlyList<long> Groups { get; }
        bool IsValidated { get; }
        string Email { get; }
        string Title { get; }
        bool IsBlocked { get; }
        DateTime? UnblockTime { get; }
        long? SiteId { get; }
        string Properties { get; }
        long? AvatarImageId { get; set; }
        long? UserImageId { get; set; }
        IReadOnlyList<Tuple<ICachedNodeInformation, NodeAccessType>> Nodes { get; }
    }
}
