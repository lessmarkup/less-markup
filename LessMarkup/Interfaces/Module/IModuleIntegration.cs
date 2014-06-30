/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Web.Mvc;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.Interfaces.Module
{
    public interface IModuleIntegration
    {
        void RegisterAction(EntityType entityType, int integrationActionId, string controllerName, string actionName);
        bool GetAction(EntityType entityType, int integrationActionId, out string controllerName, out string actionName);
        void RegisterEntityNameProvider(EntityType entityType, IEntityNameProvider entityNameProvider);
        void RegisterEntityRenderer(EntityType entityType, IEntityRenderer entityRenderer);
        string ReadEntityName(EntityType entityType, long entityId, IDomainModel domainModel);
        string ReadEntityTypeName(EntityType entityType);
        string ReadEntityLink(EntityType entityType, long entityId, UrlHelper urlHelper, IDomainModel domainModel);
        bool RenderEntity(EntityType entityType, long entityId, string highlightText, HtmlHelper htmlHelper, UrlHelper urlHelper);
        bool RenderEntity(string entity, string highlightText, HtmlHelper htmlHelper, UrlHelper urlHelper);
        void RegisterRequestHandler<T>(ModuleType moduleType, T handler);
        IEnumerable<T> GetRequestHandlers<T>();
        void RegisterCategoryHandler(ModuleType moduleType, ICategoryHandler categoryHandler);
        string GetCategoryView(ModuleType moduleType, long categoryId);
        string GetCategoryItemView(ModuleType moduleType, long categoryId);
        void InitializeCategoryModel(ModuleType moduleType, long categoryId, UrlHelper urlHelper, out object model, out string view);
        bool InitializeItemModel(ModuleType moduleType, long categoryId, string itemId, object source, UrlHelper urlHelper, out object model, out string view);
        void RegisterBackgroundJobHandler(IBackgroundJobHandler backgroundJobHandler);
        bool DoBackgroundJobs(UrlHelper urlHelper);
        void RegisterSearchResultValidator(EntityType entityType, ISearchResultValidator validator);
        bool IsSearchResultValid(SearchResult searchResult);
        void RegisterPageHandler<T>(ModuleType moduleType, string id) where T : IPageHandler;
        Tuple<Type, ModuleType> GetPageHandler(string id);
        IEnumerable<string> GetPageHandlers();
    }
}
