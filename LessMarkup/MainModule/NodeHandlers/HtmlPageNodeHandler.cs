using System;
using System.Collections.Generic;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.MainModule.Model;

namespace LessMarkup.MainModule.NodeHandlers
{
    public class HtmlPageNodeHandler : AbstractNodeHandler
    {
        protected override Dictionary<string, object> GetViewData()
        {
            var settings = GetSettings<HtmlPageModel>();

            return new Dictionary<string, object>
            {
                { "Body", settings != null ? settings.Body : "" }
            };
        }

        protected override Type SettingsModel
        {
            get { return typeof(HtmlPageModel); }
        }
    }
}
