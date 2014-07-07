/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.Structure
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ConfigurationHandlerAttribute : Attribute
    {
        public object TitleTextId { get; set; }

        public string ModuleType { get; set; }

        public bool IsGlobal { get; set; }

        public ConfigurationHandlerAttribute(object titleTextId)
        {
            TitleTextId = titleTextId;
        }
    }
}
