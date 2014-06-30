/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Articles.Model;
using LessMarkup.Articles.Module.PageHandlers;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.Articles.Module
{
    public class ArticlesModuleInitializer : BaseModuleInitializer
    {
        private readonly IModuleIntegration _moduleIntegration;

        public ArticlesModuleInitializer(IModuleIntegration moduleIntegration)
        {
            _moduleIntegration = moduleIntegration;
        }

        public override string Name
        {
            get { return "Articles"; }
        }

        public override ModuleType Type
        {
            get { return ModuleType.Articles; }
        }

        public override Type[] ModelTypes
        {
            get { return typeof (ArticleModel).Assembly.GetTypes(); }
        }

        public override void InitializeDatabase()
        {
            base.InitializeDatabase();
            _moduleIntegration.RegisterPageHandler<CommonArticlePageHandler>(ModuleType.Articles, "articles");
        }
    }
}
