/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.Module;

namespace LessMarkup.Interfaces.Data
{
    public class Module : NonSiteDataObject
    {
        public long ModuleId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Enabled { get; set; }
        public bool Removed { get; set; }
        public bool System { get; set; }
        public ModuleType Type { get; set; }
    }
}
