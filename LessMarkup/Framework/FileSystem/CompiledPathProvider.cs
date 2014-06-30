/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web.Hosting;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Framework.FileSystem
{
    public class CompiledPathProvider : VirtualPathProvider
    {
        private readonly IDataCache _dataCache;

        public CompiledPathProvider(IDataCache dataCache)
        {
            _dataCache = dataCache;
        }

        public override VirtualFile GetFile(string virtualPath)
        {
            if (virtualPath.StartsWith("~/Views/"))
            {
                return null;
            }

            if (!_dataCache.Get<ResourceCache>().ResourceExists(virtualPath))
            {
                return null;
            }

            return new CompiledVirtualFile(virtualPath, _dataCache);
        }

        public override VirtualDirectory GetDirectory(string virtualDir)
        {
            if (virtualDir.StartsWith("~/Views/"))
            {
                return null;
            }

            if (!_dataCache.Get<ResourceCache>().DirectoryExists(virtualDir))
            {
                return null;
            }

            return new CompiledVirtualDirectory(virtualDir, _dataCache);
        }

        public override bool DirectoryExists(string virtualDir)
        {
            if (virtualDir.StartsWith("~/Views/"))
            {
                return false;
            }

            if (!_dataCache.Get<ResourceCache>().DirectoryExists(virtualDir))
            {
                return false;
            }

            return true;
        }

        public override bool FileExists(string virtualPath)
        {
            if (virtualPath.StartsWith("~/Views/"))
            {
                return false;
            }

            if (!_dataCache.Get<ResourceCache>().ResourceExists(virtualPath))
            {
                return false;
            }

            return true;
        }

        public override System.Web.Caching.CacheDependency GetCacheDependency(string virtualPath, System.Collections.IEnumerable virtualPathDependencies, System.DateTime utcStart)
        {
            return null;
        }
    }
}
