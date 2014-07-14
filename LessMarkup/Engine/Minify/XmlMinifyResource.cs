using System;
using System.Xml.Serialization;

namespace LessMarkup.Engine.Minify
{
    [Serializable]
    public class XmlMinifyResource
    {
        [XmlAttribute]
        public bool Minified { get; set; }
        [XmlAttribute]
        public string Path { get; set; }
    }
}
