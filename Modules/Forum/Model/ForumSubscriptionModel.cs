using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;
using Newtonsoft.Json;

namespace LessMarkup.Forum.Model
{
    [RecordModel(TitleTextId = ForumTextIds.Subscribe)]
    public class ForumSubscriptionModel : IInputSource
    {
        [InputField(InputFieldType.MultiSelect, ForumTextIds.Forums)]
        public List<string> Forums { get; set; }

        private readonly IDataCache _dataCache;
        private readonly IDomainModelProvider _domainModelProvider;
        private readonly ICurrentUser _currentUser;

        public ForumSubscriptionModel(IDataCache dataCache, IDomainModelProvider domainModelProvider, ICurrentUser currentUser)
        {
            _dataCache = dataCache;
            _domainModelProvider = domainModelProvider;
            _currentUser = currentUser;
        }
        
        public void Load(long nodeId)
        {
            Forums = new List<string>();

            var userId = _currentUser.UserId;

            if (!userId.HasValue)
            {
                return;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var nodeUserData = domainModel.GetSiteCollection<NodeUserData>().FirstOrDefault(n => n.NodeId == nodeId && n.UserId == userId.Value);
                if (nodeUserData == null || string.IsNullOrEmpty(nodeUserData.Settings))
                {
                    return;
                }

                var settingsModel = JsonConvert.DeserializeObject<PostUpdatesUserSettingsModel>(nodeUserData.Settings);

                if (settingsModel.ForumIds == null)
                {
                    return;
                }

                Forums = settingsModel.ForumIds.Select(id => id.ToString(CultureInfo.InvariantCulture)).ToList();
            }
        }

        public void Save(long nodeId)
        {
            var userId = _currentUser.UserId;

            if (!userId.HasValue)
            {
                return;
            }

            using (var domainModel = _domainModelProvider.Create())
            {
                var nodeUserData = domainModel.GetSiteCollection<NodeUserData>().FirstOrDefault(n => n.NodeId == nodeId && n.UserId == userId.Value);

                PostUpdatesUserSettingsModel settingsModel;

                if (nodeUserData != null)
                {
                    if (!string.IsNullOrEmpty(nodeUserData.Settings))
                    {
                        settingsModel = JsonConvert.DeserializeObject<PostUpdatesUserSettingsModel>(nodeUserData.Settings);
                    }
                    else
                    {
                        settingsModel = new PostUpdatesUserSettingsModel();
                    }
                }
                else
                {
                    settingsModel = new PostUpdatesUserSettingsModel();
                    nodeUserData = new NodeUserData
                    {
                        UserId = userId.Value,
                        NodeId = nodeId
                    };
                    domainModel.AddSiteObject(nodeUserData);
                }

                if (Forums == null)
                {
                    settingsModel.ForumIds = new List<long>();
                }
                else
                {
                    settingsModel.ForumIds = Forums.Select(long.Parse).ToList();
                }

                nodeUserData.Settings = JsonConvert.SerializeObject(settingsModel);
                domainModel.SaveChanges();
            }
        }

        public List<EnumSource> GetEnumValues(string fieldName)
        {
            var userCache = _dataCache.Get<IUserCache>();

            return userCache.Nodes.Where(n => n.Item1.HandlerType.Name == "ForumNodeHandler")
                .Select(n => new EnumSource
                {
                    Text = n.Item1.Title,
                    Value = n.Item1.NodeId.ToString(CultureInfo.InvariantCulture)
                })
                .ToList();
        }
    }
}
