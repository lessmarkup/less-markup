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
            Align = Align.Left;
        }

        public string Width { get; set; }
        public string MinWidth { get; set; }
        public string MaxWidth { get; set; }
        public bool Visible { get; set; }
        public bool Sortable { get; set; }
        public bool Resizable { get; set; }
        public bool Groupable { get; set; }
        public bool Pinnable { get; set; }
        public string CellClass { get; set; }
        public string CellTemplate { get; set; }
        public string HeaderClass { get; set; }
        public object TextId { get; set; }
        public string CellUrl { get; set; }
        public bool AllowUnsafe { get; set; }
        public string Scope { get; set; }
        public Align Align { get; set; }
    }
}
