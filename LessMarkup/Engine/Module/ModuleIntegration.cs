/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.Engine.Logging;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.Engine.Module
{
    class ModuleIntegration : IModuleIntegration
    {
        private readonly List<IBackgroundJobHandler>  _backgroundJobHandlers = new List<IBackgroundJobHandler>();
        private readonly Dictionary<int, ISearchResultValidator> _searchResultValidators = new Dictionary<int, ISearchResultValidator>();
        private readonly Dictionary<string, Tuple<Type, string>> _nodeHandlers = new Dictionary<string, Tuple<Type, string>>();
        private readonly IEngineConfiguration _engineConfiguration;

        private long _lastBackgroundJobTime;

        public ModuleIntegration(IEngineConfiguration engineConfiguration)
        {
            _engineConfiguration = engineConfiguration;
        }

        public void RegisterBackgroundJobHandler(IBackgroundJobHandler backgroundJobHandler)
        {
            _backgroundJobHandlers.Add(backgroundJobHandler);
        }

        public bool DoBackgroundJobs(UrlHelper urlHelper)
        {
            if (Environment.TickCount - _lastBackgroundJobTime < _engineConfiguration.BackgroundJobInterval)
            {
                return true;
            }

            bool ret = true;

            foreach (var handler in _backgroundJobHandlers)
            {
                try
                {
                    if (!handler.DoBackgroundJob(urlHelper))
                    {
                        ret = false;
                    }
                }
                catch (Exception e)
                {
                    this.LogException(e);
                    ret = false;
                }
            }

            _lastBackgroundJobTime = Environment.TickCount;

            return ret;
        }

        public void RegisterSearchResultValidator<T>(ISearchResultValidator validator) where T : IDataObject
        {
            _searchResultValidators[AbstractDomainModel.GetCollectionId<T>()] = validator;
        }

        public bool IsSearchResultValid(SearchResult searchResult)
        {
            ISearchResultValidator validator;
            if (!_searchResultValidators.TryGetValue(searchResult.CollectionId, out validator))
            {
                return true;
            }
            return validator.IsValid(searchResult);
        }

        public void RegisterNodeHandler<T>(string id) where T : INodeHandler
        {
            if (string.IsNullOrWhiteSpace(RegisteringModuleType))
            {
                throw new ArgumentException("ModuleType is not specified");
            }

            _nodeHandlers[id] = Tuple.Create(typeof (T), RegisteringModuleType);
        }

        public Tuple<Type, string> GetNodeHandler(string id)
        {
            Tuple<Type, string> ret;
            return _nodeHandlers.TryGetValue(id, out ret) ? ret : null;
        }

        public IEnumerable<string> GetNodeHandlers()
        {
            return _nodeHandlers.Keys;
        }

        internal string RegisteringModuleType { get; set; }
    }
}
