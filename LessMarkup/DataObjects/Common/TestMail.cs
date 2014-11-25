/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.DataObjects.Common
{
    public class TestMail : DataObject
    {
        [TextSearch]
        public string From { get; set; }
        [TextSearch]
        public string To { get; set; }
        [TextSearch]
        public string Subject { get; set; }
        [TextSearch]
        public string Body { get; set; }
        public DateTime Sent { get; set; }
        public int Views { get; set; }
        [TextSearch]
        public string Template { get; set; }
    }
}
