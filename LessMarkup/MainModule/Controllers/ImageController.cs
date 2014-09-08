/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web.Mvc;
using LessMarkup.MainModule.Model;

namespace LessMarkup.MainModule.Controllers
{
    public class ImageController : Controller
    {
        public ActionResult Get(long id)
        {
            var model = Interfaces.DependencyResolver.Resolve<ImageModel>();
            return model.Get(id);
        }

        public ActionResult Thumbnail(long id)
        {
            var model = Interfaces.DependencyResolver.Resolve<ImageModel>();
            return model.Thumbnail(id);
        }

        public ActionResult Smile(long id)
        {
            var model = Interfaces.DependencyResolver.Resolve<ImageModel>();
            return model.Smile(id);
        }
    }
}
