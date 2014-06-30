/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Web.Mvc;

namespace LessMarkup.UserInterface.Model.Structure
{
    public class PageErrorModel
    {
        public void Initialize(Exception e)
        {
        }

        public ActionResult CreateResult(System.Web.Mvc.Controller controller)
        {
            return new ContentResult
            {
                Content = "Error Occurred / Description TBD",
            };
        }
    }
}
