﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Xml.Serialization;

namespace LessMarkup.Engine.Language
{
    [Serializable]
    public class XmlTranslation
    {
        [XmlAttribute]
        public string Id { get; set; }

        [XmlAttribute]
        public string Text { get; set; }
    }
}
