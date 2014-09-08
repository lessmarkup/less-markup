/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Interfaces.Structure
{
    public interface INodeCache : ICacheHandler
    {
        ICachedNodeInformation GetNode(long nodeId);
        void GetNode(string path, out ICachedNodeInformation node, out string rest);
        ICachedNodeInformation RootNode { get; }
        IReadOnlyList<ICachedNodeInformation> Nodes { get; }
        INodeHandler GetNodeHandler(string path, object controller = null, Func<INodeHandler, string, string, string, long?, bool> preprocessFunc = null);
    }
}
