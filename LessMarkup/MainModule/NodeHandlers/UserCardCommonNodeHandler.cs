using System.Collections.Generic;
using LessMarkup.DataObjects.Security;
using LessMarkup.Engine.Language;
using LessMarkup.Framework;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.NodeHandlers.Common;
using Newtonsoft.Json;

namespace LessMarkup.MainModule.NodeHandlers
{
    [UserCardHandler(MainModuleTextIds.UserCommon)]
    public class UserCardCommonNodeHandler : PropertiesNodeHandler, IUserCardNodeHandler
    {
        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly IModuleIntegration _moduleIntegration;

        public UserCardCommonNodeHandler(IDataCache dataCache, IModuleProvider moduleProvider, IDomainModelProvider domainModelProvider, IModuleIntegration moduleIntegration) : base(moduleProvider)
        {
            _dataCache = dataCache;
            _domainModelProvider = domainModelProvider;
            _moduleIntegration = moduleIntegration;
        }

        [Property(MainModuleTextIds.UserName, InputFieldType.Text)]
        public string Name { get; set; }

        [Property(MainModuleTextIds.Image, InputFieldType.Image)]
        public long? Image { get; set; }

        [Property(MainModuleTextIds.ProfileAvatar, InputFieldType.Image)]
        public long? Avatar { get; set; }

        private static InputFieldType? GetFieldType(UserPropertyType type)
        {
            switch (type)
            {
                case UserPropertyType.Date:
                    return InputFieldType.Date;
                case UserPropertyType.Image:
                    return InputFieldType.Image;
                case UserPropertyType.Note:
                    return InputFieldType.MultiLineText;
                case UserPropertyType.Text:
                    return InputFieldType.Text;
                default:
                    return null;
            }
        }

        public void Initialize(long userId)
        {
            var userCache = _dataCache.Get<IUserCache>(userId);
            Name = userCache.Name;
            Avatar = userCache.AvatarImageId;
            Image = userCache.UserImageId;

            if (!string.IsNullOrWhiteSpace(userCache.Properties))
            {
                var properties = JsonConvert.DeserializeObject<Dictionary<string, object>>(userCache.Properties);

                using (var domainModel = _domainModelProvider.Create())
                {
                    foreach (var definition in domainModel.GetSiteCollection<UserPropertyDefinition>())
                    {
                        object propertyValue;
                        if (properties.TryGetValue(definition.Name, out propertyValue))
                        {
                            var propertyType = GetFieldType(definition.Type);

                            if (propertyType.HasValue)
                            {
                                AddProperty(definition.Title, propertyType.Value, propertyValue);
                            }
                        }
                    }
                }
            }

            foreach (var property in _moduleIntegration.GetUserProperties(userId))
            {
                var propertyType = GetFieldType(property.Type);

                if (propertyType.HasValue)
                {
                    AddProperty(property.Name, propertyType.Value, property.Value);
                }
            }
        }
    }
}
