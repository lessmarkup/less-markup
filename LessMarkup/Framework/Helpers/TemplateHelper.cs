/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Web;
using System.Web.Mvc;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.System;
using DependencyResolver = LessMarkup.Interfaces.DependencyResolver;

namespace LessMarkup.Framework.Helpers
{
    public static class TemplateHelper
    {
        public static IHtmlString RenderTemplate(this HtmlHelper htmlHelper, string path)
        {
            var dataCache = DependencyResolver.Resolve<IDataCache>();
            var languageCache = dataCache.Get<ILanguageCache>();
            var templateCache = dataCache.Get<IResourceCache>(languageCache.CurrentLanguageId);
            var template = templateCache.ReadText(path);
            return htmlHelper.Raw(template);
        }

        public static IHtmlString RenderGoogleAnalytics(this HtmlHelper htmlHelper)
        {
            var configuration = DependencyResolver.Resolve<IDataCache>().Get<ISiteConfiguration>();
            var resourceId = configuration.GoogleAnalyticsResource;

            if (!String.IsNullOrWhiteSpace(resourceId))
            {
                var analyticsObject =
                    "<script>(function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){" +
                    "(i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o)," +
                    "m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)" +
                    "})(window, document, 'script', '//www.google-analytics.com/analytics.js', 'ga');";
                analyticsObject += String.Format("ga('create', '{0}', 'auto');</script>", resourceId);
                return htmlHelper.Raw(analyticsObject);
            }

            return new HtmlString("");
        }
    }
}
