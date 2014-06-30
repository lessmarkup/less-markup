/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.Structure
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RecordActionAttribute : Attribute
    {
        public object NameTextId { get; set; }

        public RecordActionAttribute(object nameTextId)
        {
            NameTextId = nameTextId;
        }

        public string Action { get; set; }
    }
}
