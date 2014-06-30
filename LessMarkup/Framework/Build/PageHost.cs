/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Web.Mvc.Razor;
using System.Web.Razor;

namespace LessMarkup.Framework.Build
{
    class PageHost : MvcWebPageRazorHost
    {
        public PageHost(string virtualPath, string physicalPath) : base(virtualPath, physicalPath)
        {
        }

        protected override RazorCodeLanguage GetCodeLanguage()
        {
            return new CSharpRazorCodeLanguage();
        }
    }
}
