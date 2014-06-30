/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;

namespace LessMarkup.Framework.Email
{
    public class Pop3Message
    {
        public string ParseError { get; set; }
        public string FromEmail { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public DateTime Created { get; set; }
        public DateTime Received { get; set; }
        public string HtmlBody { get; set; }
        public List<MessageAttachment> Attachments { get; set; } 
    }
}
