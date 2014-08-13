using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Common
{
    public class File : SiteDataObject
    {
        public string UniqueId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Data { get; set; }
    }
}
