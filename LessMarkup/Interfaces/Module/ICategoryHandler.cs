/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web.Mvc;

namespace LessMarkup.Interfaces.Module
{
    public interface ICategoryHandler
    {
        string GetCategoryView(long categoryId);
        string GetItemView(long categoryId);
        void InitializeCategoryModel(long categoryId, UrlHelper urlHelper, out object model, out string view);
        bool InitializeItemModel(long categoryId, string itemId, object source, UrlHelper urlHelper, out object model, out string view);
    }
}
