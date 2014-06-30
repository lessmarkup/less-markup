/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.RecordModel
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InputFieldAttribute : Attribute
    {
        public InputFieldAttribute(InputFieldType type)
        {
            Type = type;
        }

        public InputFieldAttribute(InputFieldType type, object textId)
        {
            TextId = textId;
            Type = type;
        }

        public object TextId { get; private set; }
        public InputFieldType Type { get; private set; }
        public bool ReadOnly { get; set; }
        public string Id { get; set; }
        public bool Required { get; set; }
        public double? Width { get; set; }
        public string ReadOnlyCondition { get; set; }
        public string VisibleCondition { get; set; }
        public object EnumTextIdBase { get; set; }
        public object DefaultValue { get; set; }
    }
}
