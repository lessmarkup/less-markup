using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Common
{
    public class SiteProperties : DataObject
    {
        public string Title { get; set; }
        public bool Enabled { get; set; }
        public string Properties { get; set; }
    }
}
