/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Autofac;
using LessMarkup.Framework.Configuration;
using LessMarkup.Framework.FileSystem;
using LessMarkup.Framework.Logging;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.System;
using DependencyResolver = LessMarkup.DataFramework.DependencyResolver;

namespace LessMarkup.Framework.Build
{
    public class ModuleProvider : IModuleProvider
    {
        private readonly List<ModuleConfiguration> _moduleConfigurations = new List<ModuleConfiguration>();
        private readonly List<Assembly> _moduleAssemblies = new List<Assembly>();
        private readonly ISpecialFolder _specialFolder = new SpecialFolder();
        private readonly IEngineConfiguration _engineConfiguration = new EngineConfiguration();

        class ControllerConfiguration
        {
            public ModuleType ModuleType { get; set; }
            public Type Type { get; set; }
        }

        private readonly Dictionary<string, ControllerConfiguration> _controllersByName = new Dictionary<string, ControllerConfiguration>();
        private readonly Dictionary<Type, ControllerConfiguration> _controllersByType = new Dictionary<Type, ControllerConfiguration>();

        protected ModuleProvider()
        {
        }

        public static IModuleProvider RegisterProvider(ContainerBuilder builder)
        {
            var provider = new ModuleProvider();
            builder.RegisterInstance(provider).As<IModuleProvider>().SingleInstance();
            builder.RegisterInstance(provider._specialFolder).As<ISpecialFolder>().SingleInstance();
            builder.RegisterInstance(provider._engineConfiguration).As<IEngineConfiguration>().SingleInstance();
            return provider;
        }

        public IEnumerable<ModuleConfiguration> Modules { get { return _moduleConfigurations; } }

        public IEnumerable<Assembly> DiscoverAndRegisterModules()
        {
            var assemblies = new HashSet<Assembly>();

            if (_engineConfiguration.SafeMode && !_engineConfiguration.DisableSafeMode)
            {
                return assemblies;
            }

            var currentPath = _specialFolder.BinaryFiles;

            foreach (var file in new DirectoryInfo(currentPath).GetFiles("*.dll"))
            {
                if (!file.FullName.EndsWith(".Module.dll"))
                {
                    continue;
                }

                var assembly = Assembly.LoadFile(file.FullName);

                var codeBase = assembly.CodeBase.ToLower();

                if (_moduleAssemblies.Any(a => a.CodeBase.ToLower() == codeBase))
                {
                    continue;
                }

                var initializerType = assembly.GetTypes().FirstOrDefault(t => !t.IsAbstract && typeof (IModuleInitializer).IsAssignableFrom(t));

                if (initializerType == null)
                {
                    continue;
                }

                RegisterModule(assembly, false, initializerType);

                assemblies.Add(assembly);

                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    assemblies.Add(Assembly.Load(reference));
                }
            }

            return assemblies;
        }

        public void UpdateModuleDatabase(IDomainModelProvider domainModelProvider)
        {
            if (domainModelProvider == null)
            {
                return;
            }

            using (var domainModel = domainModelProvider.Create())
            {
                var existingModules = new List<ModuleConfiguration>();
                foreach (var module in domainModel.GetCollection<DataFramework.DataObjects.Module>().Where(m => !m.Removed))
                {
                    var reference = _moduleConfigurations.FirstOrDefault(m => m.Path == module.Path);
                    if (reference == null)
                    {
                        module.Removed = true;
                    }
                    else
                    {
                        existingModules.Add(reference);
                        module.System = reference.System;
                        module.Type = reference.ModuleType;
                    }
                }

                foreach (var source in _moduleConfigurations.Where(m => !existingModules.Contains(m)))
                {
                    var module = new DataFramework.DataObjects.Module
                    {
                        Enabled = true,
                        Name = source.Name,
                        Path = source.Path,
                        Removed = false,
                        System = source.System,
                        Type = source.ModuleType
                    };

                    domainModel.GetCollection<DataFramework.DataObjects.Module>().Add(module);
                    domainModel.SaveChanges();
                }

                domainModel.SaveChanges();
            }
        }

        public void InitializeModules()
        {
            var modulesToRemove = new List<ModuleConfiguration>();

            foreach (var module in _moduleConfigurations)
            {
                this.LogDebug("Initializing module '" + module.Path + "'");

                var initializer = (IModuleInitializer) DependencyResolver.Resolve(module.InitializerType);

                if (initializer == null)
                {
                    this.LogDebug("Cannot load module initializer, ignoring module");
                    modulesToRemove.Add(module);
                    continue;
                }

                initializer.Initialize();

                module.Namespace = initializer.DefaultNamespace;
                module.Name = initializer.Name;
                module.Initializer = initializer;
                module.ModuleType = initializer.Type;

                foreach (var type in module.Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof (Controller))))
                {
                    var name = type.Name;

                    if (name.EndsWith("Controller"))
                    {
                        name = name.Remove(name.Length - "Controller".Length);
                    }

                    var controllerConfiguration = new ControllerConfiguration
                    {
                        ModuleType = initializer.Type,
                        Type = type
                    };

                    _controllersByName[name] = controllerConfiguration;
                    _controllersByType[type] = controllerConfiguration;
                }

                this.LogDebug("Successfully initialized module, name set to " + module.Name);
            }

            foreach (var module in modulesToRemove)
            {
                _moduleConfigurations.Remove(module);
            }
        }

        public void InitializeModulesDatabase()
        {
            foreach (var module in _moduleConfigurations)
            {
                if (module.Initializer != null)
                {
                    this.LogDebug("Initializing module " + module.Name);
                    module.Initializer.InitializeDatabase();
                }
            }
        }

        public Type GetControllerType(string controllerName)
        {
            ControllerConfiguration ret;
            if (!_controllersByName.TryGetValue(controllerName, out ret))
            {
                return null;
            }
            return ret.Type;
        }

        public ModuleType GetControllerModuleType(Type controllerType)
        {
            ControllerConfiguration ret;
            if (!_controllersByType.TryGetValue(controllerType, out ret))
            {
                return ModuleType.None;
            }
            return ret.ModuleType;
        }

        public ModuleType GetControllerModuleType(string controllerName)
        {
            ControllerConfiguration ret;
            if (!_controllersByName.TryGetValue(controllerName, out ret))
            {
                return ModuleType.None;
            }
            return ret.ModuleType;
        }

        public void RegisterModule(Assembly moduleAssembly, bool systemModule, Type initializerType)
        {
            foreach (var reference in moduleAssembly.GetReferencedAssemblies())
            {
                Assembly.Load(reference);
            }
            
            _moduleAssemblies.Add(moduleAssembly);

            var configuration = new ModuleConfiguration(moduleAssembly, systemModule, initializerType);

            _moduleConfigurations.Add(configuration);
        }
    }
}
