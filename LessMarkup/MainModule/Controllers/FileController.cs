/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web.Mvc;
using LessMarkup.MainModule.Model;

namespace LessMarkup.MainModule.Controllers
{
    public class FileController : Controller
    {
        public ActionResult Get(string id)
        {
            var model = Interfaces.DependencyResolver.Resolve<FileModel>();
            return model.GetFile(id);
        }
    }
}
