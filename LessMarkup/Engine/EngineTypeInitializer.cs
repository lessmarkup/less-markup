/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Linq;
using Autofac;
using LessMarkup.Engine.Build;
using LessMarkup.Engine.Cache;
using LessMarkup.Engine.DataChange;
using LessMarkup.Engine.Email;
using LessMarkup.Engine.Language;
using LessMarkup.Engine.Module;
using LessMarkup.Engine.Security;
using LessMarkup.Engine.Site;
using LessMarkup.Engine.TextAndSearch;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.Engine
{
    public class EngineTypeInitializer
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
            builder.RegisterType<HtmlSanitizer>().As<IHtmlSanitizer>();
            builder.RegisterType<LanguageCache>().As<ILanguageCache>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.FullName.StartsWith("Microsoft") && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("mscorlib")))
            {
                builder.RegisterAssemblyTypes(assembly);
            }
        }
    }
}
