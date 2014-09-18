/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.Interfaces.Structure
{
    public class PropertyAttribute : Attribute
    {
        public PropertyAttribute(object textId, InputFieldType type)
        {
            TextId = textId;
            Type = type;
        }

        public object TextId { get; set; }
        public InputFieldType Type { get; set; }
    }
}
