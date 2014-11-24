/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Reflection;
using LessMarkup.Forum.DataObjects;
using LessMarkup.Forum.Model;
using LessMarkup.Forum.Module.NodeHandlers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.Forum.Module
{
    public class ForumModuleInitializer : BaseModuleInitializer
    {
        private readonly IModuleIntegration _moduleIntegration;

        public ForumModuleInitializer(IModuleIntegration moduleIntegration)
        {
            _moduleIntegration = moduleIntegration;
        }

        public override string Name
        {
            get { return "Forum"; }
        }

        public override string ModuleType
        {
            get { return Constants.ModuleType.Forum; }
        }

        public override Type[] ModelTypes
        {
            get { return typeof(ForumTextIds).Assembly.GetTypes(); }
        }

        public override Assembly DataObjectsAssembly
        {
            get { return typeof(Thread).Assembly; }
        }

        public override void InitializeDatabase()
        {
            base.InitializeDatabase();
            _moduleIntegration.RegisterNodeHandler<ForumNodeHandler>("Forum");
            _moduleIntegration.RegisterNodeHandler<PostUpdatesNodeHandler>("PostUpdates");
            _moduleIntegration.RegisterNodeHandler<AllForumsNodeHandler>("AllForums");
            _moduleIntegration.RegisterEntitySearch<Post>(DependencyResolver.Resolve<PostSearch>());
            _moduleIntegration.RegisterEntitySearch<Thread>(DependencyResolver.Resolve<ThreadSearch>());
            _moduleIntegration.RegisterUserPropertyProvider(DependencyResolver.Resolve<UserPropertiesProvider>());
        }
    }
}
