/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Xml.Serialization;

namespace LessMarkup.MainModule.Model.Xml
{
    [XmlRoot(ElementName = "Language", Namespace = "http://www.lessmarkup.com/LessMarkup/Language")]
    public class LanguageImport
    {
        public string Name { get; set; }
        public string ShortName { get; set; }

        [XmlArray("Properties")]
        [XmlArrayItem("Property")]
        public List<TranslationImport> Translations { get; set; }
    }
}
