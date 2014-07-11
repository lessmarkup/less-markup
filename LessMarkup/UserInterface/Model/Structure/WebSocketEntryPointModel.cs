/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Net;
using System.Web.Mvc;
using LessMarkup.Framework.WebSockets;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class WebSocketEntryPointModel
    {
        public ActionResult HandleRequest(System.Web.Mvc.Controller controller, string path)
        {
            var loadNodeModel = Interfaces.DependencyResolver.Resolve<LoadNodeViewModel>();

            if (!loadNodeModel.Initialize(path, null, controller, false))
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            var nodeHandler = loadNodeModel.NodeHandler as AbstractWebSocketNodeHandler;

            if (nodeHandler == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);
            }

            nodeHandler.Start(controller);

            return new EmptyResult();
        }
    }
}
