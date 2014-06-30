/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Framework.FileSystem
{
    public class ViewReference
    {
        public string Path { get; set; }
        public string ClassName { get; set; }
        public bool IsSiteAssembly { get; set; }
        public bool TypeLoaded { get; set; }
        public Type Type { get; set; }
    }
}
