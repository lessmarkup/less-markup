/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;
using Autofac;
using Autofac.Integration.Mvc;
using LessMarkup.DataFramework;
using LessMarkup.Engine;
using LessMarkup.Engine.Build.View;
using LessMarkup.Engine.FileSystem;
using LessMarkup.Engine.Helpers;
using LessMarkup.Engine.Logging;
using LessMarkup.Engine.Module;
using LessMarkup.Engine.Response;
using LessMarkup.Engine.Routing;
using LessMarkup.Engine.Site;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Security;
using LessMarkup.Interfaces.System;
using LessMarkup.MainModule.Properties;
using LessMarkup.UserInterface;
using DependencyResolver = LessMarkup.Interfaces.DependencyResolver;
using IControllerFactory = LessMarkup.Interfaces.System.IControllerFactory;

namespace LessMarkup.MainModule
{
    public class CoreApplication : HttpApplication
    {
        private static Exception _fatalException;
        private static bool _initialized;
        private static readonly object _initializeLock = new object();
        private static bool _resolverInitialized;

        class ResolverCallback : IResolverCallback
        {
            private readonly IContainer _container;

            public ResolverCallback(IContainer container)
            {
                _container = container;
            }

            public T Resolve<T>()
            {
                return _container.Resolve<T>();
            }

            public object Resolve(Type type)
            {
                return _container.Resolve(type);
            }
        }

        public static void InitializeDependencyResolver()
        {
            if (_resolverInitialized)
            {
                return;
            }
            _resolverInitialized = true;
            var builder = new ContainerBuilder();
            DataFrameworkTypeInitializer.Load(builder);
            EngineTypeInitializer.Load(builder);
            builder.RegisterFilterProvider();
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly());

            foreach (var reference in Assembly.GetExecutingAssembly().GetReferencedAssemblies())
            {
                Assembly.Load(reference);
            }

            var moduleProvider = ModuleProvider.RegisterProvider(builder);

            foreach (var type in new[] {typeof (MainModuleInitializer), typeof(UserInterfaceModuleInitializer)})
            {
                moduleProvider.RegisterModule(type.Assembly, true, type);
            }

            foreach (var assembly in moduleProvider.DiscoverAndRegisterModules())
            {
                builder.RegisterAssemblyTypes(assembly);
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.FullName.StartsWith("Microsoft") && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("mscorlib")))
            {
                builder.RegisterAssemblyTypes(assembly);
            }

            var container = builder.Build();
            System.Web.Mvc.DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            DependencyResolver.SetResolver(new ResolverCallback(container));
        }

        private void ShowFatalException()
        {
            Context.Response.StatusCode = 500;
            Context.Response.StatusDescription = Resources.InternalErrorStatusCode;
            Context.Response.Write(_fatalException.Message);
            Context.ApplicationInstance.CompleteRequest();
        }

        private static string FatalExceptionMessage
        {
            get
            {
                var message = string.Empty;

                var exception = _fatalException;

                while (exception != null)
                {
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        message += Environment.NewLine + Resources.FatalExceptionMessage_InnerException + Environment.NewLine;
                    }

                    message += exception.Message;

                    if (!string.IsNullOrWhiteSpace(exception.StackTrace))
                    {
                        message += Environment.NewLine + Resources.FatalExceptionMessage_StackTrace + Environment.NewLine + exception.StackTrace;
                    }

                    exception = exception.InnerException;
                }

                return message;
            }
        }

        private static void SendFatalExceptionNotification()
        {
            var engineConfiguration = DependencyResolver.Resolve<IEngineConfiguration>();

            if (engineConfiguration == null || !engineConfiguration.SmtpConfigured || string.IsNullOrWhiteSpace(engineConfiguration.FatalErrorsEmail))
            {
                return;
            }

            var mailSender = DependencyResolver.Resolve<IMailSender>();
            mailSender.SendPlainEmail(engineConfiguration.FatalErrorsEmail, Resources.FatalErrorEmailSubject, FatalExceptionMessage);
        }

        private static void ReloadInSafeMode()
        {
            var engineConfiguration = DependencyResolver.Resolve<IEngineConfiguration>();

            if (engineConfiguration == null)
            {
                return;
            }

            if (engineConfiguration.DisableSafeMode || engineConfiguration.SafeMode)
            {
                return;
            }

            engineConfiguration.SafeMode = true;
            engineConfiguration.Save();

            HttpRuntime.UnloadAppDomain();
        }

        void OnStart()
        {
            try
            {
                InitializeDependencyResolver();

                var engineConfiguration = DependencyResolver.Resolve<IEngineConfiguration>();

                var logLevel = engineConfiguration.LogLevel;
                if (!string.IsNullOrWhiteSpace(logLevel))
                {
                    LogLevel level;
                    LoggingHelper.Level = Enum.TryParse(logLevel, true, out level) ? level : LogLevel.None;
                }
                else
                {
                    LoggingHelper.Level = LogLevel.None;
                }

                this.LogDebug("Starting application");

                var moduleProvider = DependencyResolver.Resolve<IModuleProvider>();

                moduleProvider.InitializeModules();

                var buildEngine = DependencyResolver.Resolve<IBuildEngine>();

                var isBuildEngineRecent = buildEngine.IsRecent;
                var isBuildEngineActive = buildEngine.IsActive;

                this.LogDebug("Engine: " + (isBuildEngineActive ? "Active" : "Not Active") + ", " + (isBuildEngineRecent ? "Up to date" : "Requires rebuild"));

                var lastDatabaseUpdate = engineConfiguration.LastDatabaseUpdate;

                if (!buildEngine.IsActive || (!isBuildEngineRecent && engineConfiguration.AutoRefresh))
                {
                    this.LogDebug("Building new engine");
                    var startTime = Environment.TickCount;

                    buildEngine.Build();
                    buildEngine.Activate();

                    this.LogDebug("Build finished, took " + (Environment.TickCount-startTime) + " ms");

                    lastDatabaseUpdate = null;
                }

                ViewEngines.Engines.Clear();
                ViewEngines.Engines.Add(DependencyResolver.Resolve<CompiledViewEngine>());

                var databaseConfigurationUpToDate = lastDatabaseUpdate.HasValue &&
                                                    lastDatabaseUpdate.Value > buildEngine.LastBuildTime;

                if (!string.IsNullOrWhiteSpace(engineConfiguration.Database))
                {
                    this.LogDebug("Initializing domain provider");
                    var startTime = Environment.TickCount;
                    var domainModelProvider = DependencyResolver.Resolve<IDomainModelProvider>();
                    ((IInitialize) domainModelProvider).Initialize(databaseConfigurationUpToDate);
                    this.LogDebug("Domain provider initialization took " + (Environment.TickCount - startTime) + " ms");

                    if (!databaseConfigurationUpToDate)
                    {
                        this.LogDebug("Updating modules database");
                        moduleProvider.UpdateModuleDatabase(domainModelProvider);

                        this.LogDebug("Refreshing template list");
                        buildEngine.RefreshTemplateList(domainModelProvider);

                        engineConfiguration.LastDatabaseUpdate = DateTime.UtcNow;
                        engineConfiguration.Save();
                    }
                }
                else
                {
                    this.LogWarning("Database is not configured");
                    moduleProvider.UpdateModuleDatabase(null);
                }

                this.LogDebug("Initializing modules with database");
                moduleProvider.InitializeModulesDatabase();

                this.LogDebug("Initializing controller factory");
                DependencyResolver.Resolve<IControllerFactory>().Initialize();
                HostingEnvironment.RegisterVirtualPathProvider(new CompiledPathProvider(DependencyResolver.Resolve<IDataCache>()));

                this.LogDebug("Initializing custom routes");
                RouteTable.Routes.RouteExistingFiles = true;
                DependencyResolver.Resolve<RouteConfiguration>().Create(RouteTable.Routes);

                this.LogDebug("Registering routes, bundles etc");
                RouteConfig.RegisterRoutes(RouteTable.Routes, engineConfiguration);

                this.LogDebug("Initializing site mapper");
                ((IInitialize)DependencyResolver.Resolve<ISiteMapper>()).Initialize();
            }
            catch (Exception e)
            {
                this.LogException(e);
                LoggingHelper.Flush();

                _fatalException = e;

                try
                {
                    EventLog.WriteEntry(Constants.EventLog.Source, string.Format(Resources.FatalStartupException, FatalExceptionMessage), EventLogEntryType.Error);
                    SendFatalExceptionNotification();
                    ReloadInSafeMode();
                }
                catch (Exception e2)
                {
                    EventLog.WriteEntry(Constants.EventLog.Source, string.Format(Resources.FatalStartupLoggingException, e2.Message), EventLogEntryType.Error);
                }
            }
        }

        public override void Init()
        {
            base.Init();

            BeginRequest += OnBeginRequest;
            EndRequest += OnEndRequest;
            Error += OnError;
            LogRequest += OnLogRequest;
        }

        private const string RequestStartedKey = "RequestStarted";

        private void OnBeginRequest(object sender, EventArgs eventArgs)
        {
            SiteMapperScope.ResetMapping();

            if (!_initialized)
            {
                lock (_initializeLock)
                {
                    if (!_initialized)
                    {
                        _initialized = true;
                        var startTime = Environment.TickCount;
                        OnStart();
                        this.LogDebug("Initialization took " + (Environment.TickCount - startTime) + " ms");
                    }
                }
            }

            var filterStream = new ResponseFilterStream(Context.Response.Filter);
            Context.Items[Constants.RequestItemKeys.ResponseFilter] = filterStream;
            Context.Response.Filter = filterStream;

            this.LogDebug("StartRequest from '" + Request.UserHostAddress + "' for '" + Request.Path + "'");

            Context.Items[RequestStartedKey] = Environment.TickCount;

            var requestPath = Request.Path;

            var isResourceRequest = requestPath.StartsWith("/Content/") || requestPath.StartsWith("/Images/") || requestPath.StartsWith("/Scripts/");

            if (_fatalException != null)
            {
                if (!isResourceRequest)
                {
                    this.LogDebug("Displaying fatal exception: " + _fatalException.Message);
                    ShowFatalException();
                }
                return;
            }

            var engineConfiguration = DependencyResolver.Resolve<IEngineConfiguration>();
            if ((engineConfiguration.SafeMode && !engineConfiguration.DisableSafeMode) || string.IsNullOrWhiteSpace(engineConfiguration.Database))
            {
                this.LogDebug("Working in safe mode, disabled site mapping");
                return;
            }

            var requestMapper = DependencyResolver.Resolve<IRequestMapper>();
            var dataCache = DependencyResolver.Resolve<IDataCache>();
            var currentUser = DependencyResolver.Resolve<ICurrentUser>();

            if (!requestMapper.MapRequest(dataCache))
            {
                Context.Response.StatusCode = 404;
                Context.Response.StatusDescription = Resources.NotFoundStatusCode;
                Context.ApplicationInstance.CompleteRequest();
                Context.Response.End();
                return;
            }

            currentUser.MapCurrentUser();
        }

        private void OnError(object sender, EventArgs eventArgs)
        {
            try
            {
                var exception = Server.GetLastError();
                if (exception != null)
                {
                    if (exception.GetType() == typeof (HttpException))
                    {
                        return;
                    }
                    this.LogException(exception);
                }
                StatisticsHelper.FlagError(exception != null ? exception.Message : "Unknown Exception");
            }
            finally
            {
                SiteMapperScope.ResetMapping();
            }
        }

        private void OnLogRequest(object sender, EventArgs eventArgs)
        {
            try
            {
                DependencyResolver.Resolve<StatisticsHelper>().LogRequest();
            }
            catch (Exception e)
            {
                this.LogException(e);
            }
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            try
            {
                var startedTime = Context.Items[RequestStartedKey];
                int tookMs = startedTime != null ? (Environment.TickCount - (int) startedTime) : -1;
                this.LogDebug("EndRequest; request from '" + Request.UserHostAddress + "' for '" + Request.Path + "', took " + tookMs + " ms");
            }
            finally
            {
                SiteMapperScope.ResetMapping();
            }
        }
    }
}
