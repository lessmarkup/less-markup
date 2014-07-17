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
