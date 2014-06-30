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
                if (Request.HttpMethod == "POST" && Request.ContentType.StartsWith("application/json;"))
                {
                    var model = DataFramework.DependencyResolver.Resolve<PageJsonEntryPointModel>();
                    return model.HandleRequest(this);
                }
                else
                {
                    var model = DataFramework.DependencyResolver.Resolve<PageEntryPointModel>();
                    if (!model.Initialize("/page/" + path, this))
                    {
                        return new HttpNotFoundResult();
                    }
                    return model.CreateResult(this);
                }
            }
            catch (Exception e)
            {
                this.LogException(e);
                var model = new PageErrorModel();
                model.Initialize(e);
                return model.CreateResult(this);
            }
        }
    }
}
