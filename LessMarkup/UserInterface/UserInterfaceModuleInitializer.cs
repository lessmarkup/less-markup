/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Reflection;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Module;
using LessMarkup.UserInterface.ChangeTracking;
using LessMarkup.UserInterface.NodeHandlers.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LessMarkup.UserInterface
{
    public class UserInterfaceModuleInitializer : BaseModuleInitializer
    {
        public RecordChangeTracker RecordChangeTracker { get; private set; }

        private readonly IModuleIntegration _moduleIntegration;

        public UserInterfaceModuleInitializer(IModuleIntegration moduleIntegration)
        {
            _moduleIntegration = moduleIntegration;

            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings();
                settings.Converters.Add(new StringEnumConverter());
                return settings;
            };
            RecordChangeTracker = DependencyResolver.Resolve<RecordChangeTracker>();
        }

        public override string Name
        {
            get { return "UserInterface"; }
        }

        public override string ModuleType
        {
            get { return DataFramework.Constants.ModuleType.UserInterface; }
        }

        public override Type[] ModelTypes
        {
            get { return Assembly.GetExecutingAssembly().GetTypes(); }
        }

        public override void InitializeDatabase()
        {
            base.InitializeDatabase();
            RecordChangeTracker.Initialize();
            _moduleIntegration.RegisterNodeHandler<FlatPageNodeHandler>("flatpage");
        }
    }
}
