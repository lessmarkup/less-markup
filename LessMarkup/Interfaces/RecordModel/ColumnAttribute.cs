/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.RecordModel
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public ColumnAttribute(object textId)
        {
            Visible = true;
            Groupable = true;
            Sortable = true;
            Resizable = true;
            Pinnable = true;
            TextId = textId;
        }

        public int? WidthPercents { get; set; }
        public int? WidthPixels { get; set; }
        public int? WidthWeight { get; set; }
        public int? MinWidth { get; set; }
        public int? MaxWidth { get; set; }
        public bool Visible { get; set; }
        public bool Sortable { get; set; }
        public bool Resizable { get; set; }
        public bool Groupable { get; set; }
        public bool Pinnable { get; set; }
        public string CellClass { get; set; }
        public string HeaderClass { get; set; }
        public object TextId { get; set; }
    }
}
