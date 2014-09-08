/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web.Mvc;
using LessMarkup.MainModule.Model;

namespace LessMarkup.MainModule.Controllers
{
    public class AccountController : Controller
    {
        public ActionResult Validate(string secret)
        {
            var model = Interfaces.DependencyResolver.Resolve<UserValidateModel>();
            model.ValidateSecret(secret, Url);
            return View(model);
        }

        public ActionResult Approve(string secret)
        {
            var model = Interfaces.DependencyResolver.Resolve<ValidateApprovalModel>();
            model.ValidateSecret(secret);
            return View(model);
        }
    }
}
