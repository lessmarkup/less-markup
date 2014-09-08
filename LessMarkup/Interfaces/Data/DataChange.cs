/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Interfaces.Data
{
    public interface IDataChange
    {
        long Id { get; }
        long EntityId { get; }
        DateTime Created { get; }
        EntityChangeType Type { get; }
        long? UserId { get; }
        long Parameter1 { get; }
        long Parameter2 { get; }
        long Parameter3 { get; }
    }
}
