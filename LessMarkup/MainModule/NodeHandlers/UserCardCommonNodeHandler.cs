using LessMarkup.Engine.Language;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.MainModule.NodeHandlers
{
    [UserCardHandler(MainModuleTextIds.UserCommon)]
    public class UserCardCommonNodeHandler : PropertiesNodeHandler, IUserCardNodeHandler
    {
        private readonly IDataCache _dataCache;

        public UserCardCommonNodeHandler(IDataCache dataCache, IModuleProvider moduleProvider) : base(moduleProvider)
        {
            _dataCache = dataCache;
        }

        [Property(MainModuleTextIds.UserName)]
        public string Name { get; set; }

        public void Initialize(long userId)
        {
            var userCache = _dataCache.Get<IUserCache>(userId);
            Name = userCache.Name;
        }
    }
}
