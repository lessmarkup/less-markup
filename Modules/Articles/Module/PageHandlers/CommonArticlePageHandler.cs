/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using LessMarkup.Articles.Model;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.Articles.Module.PageHandlers
{
    public class CommonArticlePageHandler : AbstractPageHandler
    {
        public override object GetViewData(long objectId, Dictionary<string, string> settings)
        {
            return new
            {
                Body = settings["Body"]
            };
        }

        public override Type SettingsModel
        {
            get { return typeof (ArticleModel); }
        }
    }
}
