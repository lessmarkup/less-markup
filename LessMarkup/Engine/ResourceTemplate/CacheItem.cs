/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;

namespace LessMarkup.Engine.ResourceTemplate
{
    class CacheItem
    {
        public List<string> TextParts { get; set; }
        public List<Directive> Directives { get; set; }
        public string Path { get; set; }
        public string ModuleType { get; set; }
    }
}
