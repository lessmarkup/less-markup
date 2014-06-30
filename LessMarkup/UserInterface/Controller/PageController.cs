/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Web.Mvc;
using LessMarkup.Framework.Logging;
using LessMarkup.UserInterface.Model.Structure;

namespace LessMarkup.UserInterface.Controller
{
    public class PageController : System.Web.Mvc.Controller
    {
        [Framework.Routing.Route("Page", "{*path}")]
        public ActionResult EntryPoint(string path)
        {
            try
            {
                if (PageJsonEntryPointModel.AppliesToRequest(Request))
                {
                    var jsonModel = DataFramework.DependencyResolver.Resolve<PageJsonEntryPointModel>();
                    return jsonModel.HandleRequest(this);
                }

                var pageModel = DataFramework.DependencyResolver.Resolve<PageEntryPointModel>();
                if (pageModel.Initialize(path, this))
                {
                    return pageModel.CreateResult(this);
                }

                var resourceModel = DataFramework.DependencyResolver.Resolve<ResourceModel>();
                if (resourceModel.Initialize(path))
                {
                    return resourceModel.CreateResult(this);
                }

                return new HttpNotFoundResult();
            }
            catch (Exception e)
            {
                this.LogException(e);
                var model = DataFramework.DependencyResolver.Resolve<PageErrorModel>();
                model.Initialize(e);
                return model.CreateResult(this);
            }
        }
    }
}
