/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Interfaces.System
{
    public interface IResourceCache : ICacheHandler
    {
        bool ResourceExists(string path);
        Stream ReadResource(string path);
        string ReadText(string path);
        bool DirectoryExists(string path);
        List<string> GetFiles(string path);
        Type LoadType(string path);
    }
}
