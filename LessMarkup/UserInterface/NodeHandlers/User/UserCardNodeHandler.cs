using System;
using System.Reflection;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.UserInterface.NodeHandlers.User
{
    public class UserCardNodeHandler : TabPageNodeHandler
    {
        private readonly IModuleProvider _moduleProvider;
        private long _userId;

        public UserCardNodeHandler(IDataCache dataCache, ICurrentUser currentUser, IModuleProvider moduleProvider)
            : base(dataCache, currentUser)
        {
            _moduleProvider = moduleProvider;
        }

        public void Initialize(long userId)
        {
            _userId = userId;

            foreach (var module in _moduleProvider.Modules)
            {
                foreach (var type in module.Assembly.GetTypes())
                {
                    if (!typeof(INodeHandler).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    var handlerAttribute = type.GetCustomAttribute<UserCardHandlerAttribute>();

                    if (handlerAttribute == null)
                    {
                        continue;
                    }

                    AddPage(type, LanguageHelper.GetText(module.ModuleType, handlerAttribute.TitleTextId), handlerAttribute.Path, userId);
                }
            }
        }

        protected override INodeHandler CreateChildHandler(Type handlerType)
        {
            var ret = base.CreateChildHandler(handlerType);

            var userCardNodeHandler = (IUserCardNodeHandler) ret;

            if (userCardNodeHandler != null)
            {
                userCardNodeHandler.Initialize(_userId);
            }

            return ret;
        }
    }
}
