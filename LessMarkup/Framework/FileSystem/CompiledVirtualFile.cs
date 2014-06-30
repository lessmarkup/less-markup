/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.IO;
using System.Web.Hosting;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Framework.FileSystem
{
    class CompiledVirtualFile : VirtualFile
    {
        private readonly IDataCache _dataCache;
        private readonly string _virtualPath;

        public CompiledVirtualFile(string virtualPath, IDataCache dataCache) : base(virtualPath)
        {
            _dataCache = dataCache;
            _virtualPath = virtualPath;
        }

        public override Stream Open()
        {
            return _dataCache.Get<ResourceCache>().ReadResource(_virtualPath);
        }

        public override bool IsDirectory
        {
            get { return false; }
        }
    }
}
