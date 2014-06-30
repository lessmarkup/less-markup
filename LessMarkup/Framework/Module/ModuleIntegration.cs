/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using LessMarkup.Framework.Logging;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.System;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.Framework.Module
{
    class ModuleIntegration : IModuleIntegration
    {
        private readonly Dictionary<Tuple<EntityType, int>, Tuple<string, string>> _actions = new Dictionary<Tuple<EntityType, int>, Tuple<string, string>>(); 
        private readonly Dictionary<EntityType, IEntityNameProvider> _entityNameProviders = new Dictionary<EntityType, IEntityNameProvider>();
        private readonly Dictionary<EntityType, IEntityRenderer> _entityRenderers = new Dictionary<EntityType, IEntityRenderer>();
        private readonly Dictionary<ModuleType, Dictionary<Type, object>> _requestHandlers = new Dictionary<ModuleType, Dictionary<Type, object>>();
        private readonly Dictionary<ModuleType, ICategoryHandler> _categoryHandlers = new Dictionary<ModuleType, ICategoryHandler>(); 
        private readonly List<IBackgroundJobHandler>  _backgroundJobHandlers = new List<IBackgroundJobHandler>();
        private readonly Dictionary<EntityType, ISearchResultValidator> _searchResultValidators = new Dictionary<EntityType, ISearchResultValidator>();
        private readonly Dictionary<string, Tuple<Type,ModuleType>> _nodeHandlers = new Dictionary<string, Tuple<Type, ModuleType>>();

        private readonly IEngineConfiguration _engineConfiguration;
        private readonly ISiteMapper _siteMapper;

        private long _lastBackgroundJobTime;

        public ModuleIntegration(IEngineConfiguration engineConfiguration, ISiteMapper siteMapper)
        {
            _engineConfiguration = engineConfiguration;
            _siteMapper = siteMapper;
        }

        public void RegisterAction(EntityType entityType, int integrationActionId, string controllerName, string actionName)
        {
            _actions[Tuple.Create(entityType, integrationActionId)] = Tuple.Create(controllerName, actionName);
        }

        public bool GetAction(EntityType entityType, int integrationActionId, out string controllerName, out string actionName)
        {
            Tuple<string, string> action;
            if (!_actions.TryGetValue(Tuple.Create(entityType, integrationActionId), out action))
            {
                controllerName = null;
                actionName = null;
                return false;
            }

            controllerName = action.Item1;
            actionName = action.Item2;
            return true;
        }

        public void RegisterEntityNameProvider(EntityType entityType, IEntityNameProvider entityNameProvider)
        {
            _entityNameProviders[entityType] = entityNameProvider;
        }

        public void RegisterEntityRenderer(EntityType entityType, IEntityRenderer entityRenderer)
        {
            _entityRenderers[entityType] = entityRenderer;
        }

        public string ReadEntityName(EntityType entityType, long entityId, IDomainModel domainModel)
        {
            IEntityNameProvider entityNameProvider;
            if (!_entityNameProviders.TryGetValue(entityType, out entityNameProvider))
            {
                return "(" + entityType + ")";
            }
            return entityNameProvider.Read(domainModel, entityType, entityId);
        }

        public string ReadEntityTypeName(EntityType entityType)
        {
            IEntityNameProvider entityNameProvider;
            if (!_entityNameProviders.TryGetValue(entityType, out entityNameProvider))
            {
                return entityType.ToString();
            }
            return entityNameProvider.GetEntityTypeName(entityType) ?? entityType.ToString();
        }

        public string ReadEntityLink(EntityType entityType, long entityId, UrlHelper urlHelper, IDomainModel domainModel)
        {
            IEntityNameProvider entityNameProvider;
            if (!_entityNameProviders.TryGetValue(entityType, out entityNameProvider))
            {
                return null;
            }
            return entityNameProvider.EntityLink(urlHelper, entityType, entityId, domainModel);
        }

        public bool RenderEntity(EntityType entityType, long entityId, string highlightText, HtmlHelper htmlHelper, UrlHelper urlHelper)
        {
            IEntityRenderer entityRenderer;
            if (!_entityRenderers.TryGetValue(entityType, out entityRenderer))
            {
                return false;
            }
            return entityRenderer.Render(entityType, entityId, highlightText, htmlHelper, urlHelper);
        }

        public bool RenderEntity(string entity, string highlightText, HtmlHelper htmlHelper, UrlHelper urlHelper)
        {
            string[] parts = entity.Split(new[] { ',' });

            if (parts.Length == 2)
            {
                return RenderEntity((EntityType)Enum.Parse(typeof(EntityType), parts[0]), long.Parse(parts[1]), highlightText, htmlHelper, urlHelper);
            }

            return false;
        }

        public void RegisterCategoryHandler(ModuleType moduleType, ICategoryHandler categoryHandler)
        {
            _categoryHandlers[moduleType] = categoryHandler;
        }

        public string GetCategoryView(ModuleType moduleType, long categoryId)
        {
            ICategoryHandler handler;
            if (!_categoryHandlers.TryGetValue(moduleType, out handler))
            {
                return null;
            }
            return handler.GetCategoryView(categoryId);
        }

        public string GetCategoryItemView(ModuleType moduleType, long categoryId)
        {
            ICategoryHandler handler;
            if (!_categoryHandlers.TryGetValue(moduleType, out handler))
            {
                return null;
            }
            return handler.GetItemView(categoryId);
        }

        public void InitializeCategoryModel(ModuleType moduleType, long categoryId, UrlHelper urlHelper, out object model, out string view)
        {
            ICategoryHandler handler;
            if (!_categoryHandlers.TryGetValue(moduleType, out handler))
            {
                model = null;
                view = null;
                return;
            }

            handler.InitializeCategoryModel(categoryId, urlHelper, out model, out view);
        }

        public bool InitializeItemModel(ModuleType moduleType, long categoryId, string itemId, object source, UrlHelper urlHelper, out object model, out string view)
        {
            ICategoryHandler handler;
            if (!_categoryHandlers.TryGetValue(moduleType, out handler))
            {
                model = null;
                view = null;
                return false;
            }
            return handler.InitializeItemModel(categoryId, itemId, source, urlHelper, out model, out view);
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

        public void RegisterSearchResultValidator(EntityType entityType, ISearchResultValidator validator)
        {
            _searchResultValidators[entityType] = validator;
        }

        public bool IsSearchResultValid(SearchResult searchResult)
        {
            ISearchResultValidator validator;
            if (!_searchResultValidators.TryGetValue(searchResult.EntityType, out validator))
            {
                return true;
            }
            return validator.IsValid(searchResult);
        }

        public void RegisterNodeHandler<T>(ModuleType moduleType, string id) where T : INodeHandler
        {
            _nodeHandlers[id] = Tuple.Create(typeof (T), moduleType);
        }

        public Tuple<Type, ModuleType> GetNodeHandler(string id)
        {
            Tuple<Type, ModuleType> ret;
            return _nodeHandlers.TryGetValue(id, out ret) ? ret : null;
        }

        public IEnumerable<string> GetNodeHandlers()
        {
            return _nodeHandlers.Keys;
        }

        public void RegisterRequestHandler<T>(ModuleType moduleType, T handler)
        {
            Dictionary<Type, object> handlers;

            if (!_requestHandlers.TryGetValue(moduleType, out handlers))
            {
                handlers = new Dictionary<Type, object>();
                _requestHandlers[moduleType] = handlers;
            }

            handlers[typeof (T)] = handler;
        }

        public IEnumerable<T> GetRequestHandlers<T>()
        {
            var allowedModules = _siteMapper.ModuleTypes;

            if (allowedModules == null)
            {
                yield break;
            }

            foreach (var moduleType in allowedModules)
            {
                Dictionary<Type, object> handlers;

                if (!_requestHandlers.TryGetValue(moduleType, out handlers))
                {
                    continue;
                }

                object handler;
                if (!handlers.TryGetValue(typeof (T), out handler))
                {
                    continue;
                }

                yield return (T) handler;
            }
        }
    }
}
