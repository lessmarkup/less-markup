using System;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Common
{
    public class MigrationHistory : DataObject
    {
        public string ModuleType { get; set; }
        public string UniqueId { get; set; }
        public DateTime Created { get; set; }
    }
}
