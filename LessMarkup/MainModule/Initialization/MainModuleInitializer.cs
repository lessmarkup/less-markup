/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using System.Reflection;
using LessMarkup.DataObjects.Security;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Engine;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Module;
using LessMarkup.MainModule.NodeHandlers;

namespace LessMarkup.MainModule.Initialization
{
    public class MainModuleInitializer : BaseModuleInitializer
    {
        private readonly IModuleIntegration _moduleIntegration;

        public MainModuleInitializer(IModuleIntegration moduleIntegration)
        {
            _moduleIntegration = moduleIntegration;
        }

        public override string Name { get { return "MainModule"; } }

        public override string ModuleType
        {
            get { return DataFramework.Constants.ModuleType.MainModule; }
        }

        public override Type[] ModelTypes
        {
            get
            {
                var modelTypes = Assembly.GetExecutingAssembly().GetTypes().ToList();
                modelTypes.AddRange(typeof(EngineTypeInitializer).Assembly.GetTypes());
                return modelTypes.ToArray();
            }
        }

        public override Assembly DataObjectsAssembly
        {
            get
            {
                return typeof(DataObjects.Migrations.Initial).Assembly;
            }
        }

        public override void InitializeDatabase()
        {
            base.InitializeDatabase();

            _moduleIntegration.RegisterNodeHandler<ArticleNodeHandler>("article");
            _moduleIntegration.RegisterNodeHandler<ContactFormNodeHandler>("contact");
            _moduleIntegration.RegisterNodeHandler<HtmlPageNodeHandler>("htmlpage");
            _moduleIntegration.RegisterEntitySearch<Node>(DependencyResolver.Resolve<NodeSearch>());
            _moduleIntegration.RegisterEntitySearch<User>(DependencyResolver.Resolve<UserSearch>());
        }

        public override string DefaultNamespace
        {
            get { return "LessMarkup.MainModule"; }
        }
    }
}
