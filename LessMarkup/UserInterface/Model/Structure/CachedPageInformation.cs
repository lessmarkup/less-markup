/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class CachedPageInformation
    {
        public long PageId { get; set; }
        public bool Enabled { get; set; }
        public string Path { get; set; }
        public int Order { get; set; }
        public int Level { get; set; }
        public string Title { get; set; }
        public string HandlerId { get; set; }
        public long? ParentPageId { get; set; }
        public CachedPageInformation Parent { get; set; }
        public List<CachedPageAccess> AccessList { get; set; }
        public List<CachedPageInformation> Children { get; set; }
        public string FullPath { get; set; }
        public Type HandlerType { get; set; }
        public ModuleType HandlerModuleType { get; set; }
        public string Settings { get; set; }
        public CachedPageInformation Root { get; set; }
    }
}
