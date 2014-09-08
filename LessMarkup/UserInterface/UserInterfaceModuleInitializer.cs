/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Reflection;
using Autofac;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.Structure;
using LessMarkup.UserInterface.NodeHandlers.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace LessMarkup.UserInterface
{
    public class UserInterfaceModuleInitializer : BaseModuleInitializer
    {
        private readonly IModuleIntegration _moduleIntegration;

        public UserInterfaceModuleInitializer(IModuleIntegration moduleIntegration)
        {
            _moduleIntegration = moduleIntegration;

            JsonConvert.DefaultSettings = () =>
            {
                var settings = new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
                settings.Converters.Add(new StringEnumConverter());
                return settings;
            };
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
            _moduleIntegration.RegisterNodeHandler<FlatPageNodeHandler>("flatpage");
        }

        public static void Load(ContainerBuilder builder)
        {
            builder.RegisterType<NodeCache>().As<INodeCache>();
        }
    }
}
