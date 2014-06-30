/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.Hosting;
using LessMarkup.Interfaces.Cache;

namespace LessMarkup.Framework.FileSystem
{
    class CompiledVirtualDirectory : VirtualDirectory
    {
        private readonly IDataCache _dataCache;
        private readonly List<string> _directories;
        private readonly List<string> _files;
        private readonly string _virtualPath;

        public CompiledVirtualDirectory(string virtualPath, IDataCache dataCache) : base(virtualPath)
        {
            _dataCache = dataCache;
            var resourceCache = _dataCache.Get<ResourceCache>();
            _directories = resourceCache.GetDirectories(virtualPath);
            _files = resourceCache.GetFiles(virtualPath);
            _virtualPath = virtualPath;
            if (!_virtualPath.EndsWith("/"))
            {
                _virtualPath += "/";
            }
        }

        public override IEnumerable Directories
        {
            get { return _directories.Select(d => new CompiledVirtualDirectory(_virtualPath + d, _dataCache)); }
        }

        public override IEnumerable Files
        {
            get { return _files.Select(f => new CompiledVirtualFile(_virtualPath + f, _dataCache)); }
        }

        public override IEnumerable Children
        {
            get
            {
                var ret = new List<object>();
                ret.AddRange(_directories.Select(d => (object) new CompiledVirtualDirectory(_virtualPath + d, _dataCache)));
                ret.AddRange(_files.Select(f => (object) new CompiledVirtualFile(_virtualPath + f, _dataCache)));
                return ret;
            }
        }
    }
}
