/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Reflection;

namespace LessMarkup.Interfaces.RecordModel
{
    public class ColumnDefinition
    {
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
        public string CellTemplate { get; set; }
        public object TextId { get; set; }
        public PropertyInfo Property { get; set; }

        public void Initialize(ColumnAttribute configuration, PropertyInfo property)
        {
            WidthPercents = configuration.WidthPercents;
            WidthPixels = configuration.WidthPixels;
            WidthWeight = configuration.WidthWeight;
            MinWidth = configuration.MinWidth;
            MaxWidth = configuration.MaxWidth;
            Visible = configuration.Visible;
            Sortable = configuration.Sortable;
            Resizable = configuration.Resizable;
            Groupable = configuration.Groupable;
            Pinnable = configuration.Pinnable;
            CellClass = configuration.CellClass;
            HeaderClass = configuration.HeaderClass;
            TextId = configuration.TextId;
            Property = property;
            CellTemplate = configuration.CellTemplate;
        }
    }
}
