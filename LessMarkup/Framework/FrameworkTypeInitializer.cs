/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using Autofac;
using LessMarkup.Framework.Build;
using LessMarkup.Framework.Cache;
using LessMarkup.Framework.DataChange;
using LessMarkup.Framework.Email;
using LessMarkup.Framework.Module;
using LessMarkup.Framework.Security;
using LessMarkup.Framework.Site;
using LessMarkup.Framework.TextAndSearch;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.Framework
{
    public class FrameworkTypeInitializer
    {
        public static void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ModuleIntegration>().As<IModuleIntegration>().SingleInstance();
            builder.RegisterType<MailTemplateProvider>().As<IMailTemplateProvider>();
            builder.RegisterType<TextSearchEngine>().As<ITextSearch>().SingleInstance();
            builder.RegisterType<DataCache>().As<IDataCache>().SingleInstance();
            builder.RegisterType<UserSecurity>().As<IUserSecurity>();
            builder.RegisterType<SiteMapper>().As<ISiteMapper>().As<IRequestMapper>().SingleInstance();
            builder.RegisterType<BuildEngine>().As<IBuildEngine>();
            builder.RegisterType<ControllerFactory>().As<IControllerFactory>();
            builder.RegisterType<MailSender>().As<IMailSender>();
            builder.RegisterType<ChangeTracker>().As<IChangeTracker>().SingleInstance();
            builder.RegisterType<CurrentUser>().As<ICurrentUser>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.FullName.StartsWith("Microsoft") && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("mscorlib")))
            {
                builder.RegisterAssemblyTypes(assembly);
            }
        }
    }
}
