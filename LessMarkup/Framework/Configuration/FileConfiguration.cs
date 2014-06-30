/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Xml.Serialization;

namespace LessMarkup.Framework.Configuration
{
    [XmlRoot(ElementName = "Configuration", Namespace = "http://www.lessmarkup.com/LessMarkup/EngineConfiguration")]
    public class FileConfiguration
    {
        [XmlArray("Properties")]
        [XmlArrayItem("Property")]
        public List<FileConfigurationProperty> Properties { get; set; }
    }
}
