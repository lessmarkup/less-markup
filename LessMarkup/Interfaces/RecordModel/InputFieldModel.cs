/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;

namespace LessMarkup.Interfaces.RecordModel
{
    public class InputFieldModel
    {
        public string Text { get; set; }
        public InputFieldType Type { get; set; }
        public bool ReadOnly { get; set; }
        public string Id { get; set; }
        public bool Required { get; set; }
        public double? Width { get; set; }
        public string ReadOnlyCondition { get; set; }
        public string VisibleCondition { get; set; }
        public string Property { get; set; }
        public string HelpText { get; set; }
        public List<SelectValueModel> SelectValues { get; set; }
        public object DefaultValue { get; set; }
    }
}
