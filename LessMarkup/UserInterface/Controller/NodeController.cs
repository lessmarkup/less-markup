/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Web.Mvc;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.System;
using LessMarkup.UserInterface.Model.Structure;
using DependencyResolver = LessMarkup.Interfaces.DependencyResolver;

namespace LessMarkup.UserInterface.Controller
{
    public class NodeController : System.Web.Mvc.Controller
    {
        [Engine.Routing.Route("Node", "{*path}")]
        public ActionResult NodeEntryPoint(string path)
        {
            this.LogDebug("Request to '" + path + "'");
            try
            {
                if (path != null && path.StartsWith("language/"))
                {
                    this.LogDebug("Handling language change request");
                    long languageId;
                    if (!long.TryParse(path.Substring("language/".Length), out languageId))
                    {
                        this.LogDebug("Unknown language id");
                        return HttpNotFound();
                    }
                    var dataCache = DependencyResolver.Resolve<IDataCache>();
                    dataCache.Get<ILanguageCache>().CurrentLanguageId = languageId;
                    return Redirect("/");
                }

                if (HttpContext.IsWebSocketRequest)
                {
                    this.LogDebug("Switching to WebSocket API");
                    var model = DependencyResolver.Resolve<WebSocketEntryPointModel>();
                    return model.HandleRequest(this, path);
                }

                if (JsonEntryPointModel.AppliesToRequest(Request, path))
                {
                    this.LogDebug("Start of JSON request");
                    var jsonModel = DependencyResolver.Resolve<JsonEntryPointModel>();
                    var ret = jsonModel.HandleRequest(this);
                    this.LogDebug("End of JSON request");
                    return ret;
                }

                var nodeModel = DependencyResolver.Resolve<NodeEntryPointModel>();
                if (nodeModel.Initialize(path, this))
                {
                    this.LogDebug("Handling node access request");
                    return nodeModel.CreateResult(this);
                }

                var resourceModel = DependencyResolver.Resolve<ResourceModel>();
                if (resourceModel.Initialize(path))
                {
                    this.LogDebug("Handling resource access request");
                    return resourceModel.CreateResult(this);
                }

                this.LogDebug("Unknown path");
                return new HttpNotFoundResult();
            }
            catch (Exception e)
            {
                this.LogException(e);
                var model = DependencyResolver.Resolve<NodeErrorModel>();
                model.Initialize(e);
                return model.CreateResult(this);
            }
        }
    }
}
