/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Xml.Serialization;

namespace LessMarkup.Engine.Minify
{
    [Serializable]
    public class XmlMinifyResource
    {
        [XmlAttribute]
        public string Minified { get; set; }
        [XmlAttribute]
        public string Plain { get; set; }
    }
}
