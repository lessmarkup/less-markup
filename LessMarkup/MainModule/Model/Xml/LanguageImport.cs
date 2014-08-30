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
