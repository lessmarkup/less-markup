/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Forum.Model;
using LessMarkup.Forum.Module.NodeHandlers;
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

        public override void InitializeDatabase()
        {
            base.InitializeDatabase();
            _moduleIntegration.RegisterNodeHandler<ForumNodeHandler>("Forum");
        }
    }
}
