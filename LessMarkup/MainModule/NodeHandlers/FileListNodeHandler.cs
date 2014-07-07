using LessMarkup.Engine.Language;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;
using LessMarkup.MainModule.Model;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.MainModule.NodeHandlers
{
    [ConfigurationHandler(MainModuleTextIds.Files)]
    public class FileListNodeHandler : RecordListNodeHandler<FileModel>
    {
        public FileListNodeHandler(IDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
        }
    }
}
