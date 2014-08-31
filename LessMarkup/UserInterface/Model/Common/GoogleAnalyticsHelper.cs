using System.Web;
using System.Web.Mvc;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.System;

namespace LessMarkup.UserInterface.Model.Common
{
    public static class GoogleAnalyticsHelper
    {
        public static IHtmlString RenderGoogleAnalytics(this HtmlHelper htmlHelper)
        {
            var configuration = Interfaces.DependencyResolver.Resolve<IDataCache>().Get<ISiteConfiguration>();
            var resourceId = configuration.GoogleAnalyticsResource;

            if (!string.IsNullOrWhiteSpace(resourceId))
            {
                var analyticsObject =
                    "<script>(function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){" +
                    "(i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o)," +
                    "m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)" +
                    "})(window, document, 'script', '//www.google-analytics.com/analytics.js', 'ga');";
                analyticsObject += string.Format("ga('create', '{0}', 'auto');</script>", resourceId);
                return htmlHelper.Raw(analyticsObject);
            }

            return new HtmlString("");
        }
    }
}
