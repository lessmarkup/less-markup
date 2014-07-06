/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Reflection;
using LessMarkup.Engine;
using LessMarkup.Interfaces.Module;
using LessMarkup.MainModule.NodeHandlers;

namespace LessMarkup.MainModule
{
    public class MainModuleInitializer : BaseModuleInitializer
    {
        private readonly IModuleIntegration _moduleIntegration;

        public MainModuleInitializer(IModuleIntegration moduleIntegration)
        {
            _moduleIntegration = moduleIntegration;
        }

        public override string Name { get { return "MainModule"; } }

        public override ModuleType Type
        {
            get { return ModuleType.MainModule; }
        }

        public override Type[] ModelTypes
        {
            get
            {
                var modelTypes = Assembly.GetExecutingAssembly().GetTypes().ToList();
                modelTypes.AddRange(typeof(FrameworkTypeInitializer).Assembly.GetTypes());
                return modelTypes.ToArray();
            }
        }

        public override void InitializeDatabase()
        {
            base.InitializeDatabase();

            _moduleIntegration.RegisterNodeHandler<ArticleNodeHandler>(ModuleType.MainModule, "article");
            _moduleIntegration.RegisterNodeHandler<ContactFormNodeHandler>(ModuleType.MainModule, "contact");
        }
    }
}
