/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using LessMarkup.Interfaces.Security;

namespace LessMarkup.Interfaces.Structure
{
    public interface ICachedNodeInformation
    {
        long NodeId { get; }
        bool Enabled { get; }
        string Path { get; }
        int Order { get; }
        int Level { get; }
        string Title { get; }
        string Description { get; set; }
        string HandlerId { get; }
        ICachedNodeInformation Parent { get; }
        List<CachedNodeAccess> AccessList { get; }
        IReadOnlyList<ICachedNodeInformation> Children { get; }
        string FullPath { get; }
        Type HandlerType { get; }
        string HandlerModuleType { get; }
        string Settings { get; }
        ICachedNodeInformation Root { get; }
        bool Visible { get; }
        bool AddToMenu { get; }
        bool LoggedIn { get; }
        NodeAccessType CheckRights(ICurrentUser currentUser, NodeAccessType defaultAccessType = NodeAccessType.Read);
    }
}
