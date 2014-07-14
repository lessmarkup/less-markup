using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace LessMarkup.Engine.Minify
{
    [Serializable]
    [XmlRoot("Minify")]
    public class XmlMinifyFile
    {
        [XmlArray("Resources")]
        [XmlArrayItem("Resource")]
        public List<XmlMinifyResource> Resources { get; set; }
    }
}
