﻿using LessMarkup.Engine.Configuration;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.NodeHandlers.GlobalConfiguration
{
    public class AdministratorLoginNodeHandler : AbstractNodeHandler
    {
        private readonly IDataCache _dataCache;
        private readonly IEngineConfiguration _engineConfiguration;
        private readonly ISiteMapper _siteMapper;

        public AdministratorLoginNodeHandler(IDataCache dataCache, IEngineConfiguration engineConfiguration, ISiteMapper siteMapper)
        {
            _dataCache = dataCache;
            _engineConfiguration = engineConfiguration;
            _siteMapper = siteMapper;
        }

        protected override object GetViewData()
        {
            string adminLoginPage;

            if (_siteMapper.SiteId.HasValue)
            {
                var siteConfiguration = _dataCache.Get<SiteConfigurationCache>();
                adminLoginPage = siteConfiguration.AdminLoginPage;
            }
            else
            {
                adminLoginPage = _engineConfiguration.AdminLoginPage;
            }

            return new
            {
                AdministratorKey = adminLoginPage
            };
        }
    }
}
