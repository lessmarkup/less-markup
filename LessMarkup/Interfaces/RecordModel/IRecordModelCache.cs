/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Interfaces.RecordModel
{
    public interface IRecordModelCache : ICacheHandler
    {
        IRecordModelDefinition GetDefinition<T>();
        IRecordModelDefinition GetDefinition(Type type);
        IRecordModelDefinition GetDefinition(string id);
        bool HasDefinition(Type type);
    }
}
