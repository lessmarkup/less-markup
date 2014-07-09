﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Configuration;
using System.Web.Mvc;
using Autofac;
using LessMarkup.Engine.Configuration;
using LessMarkup.Engine.FileSystem;
using LessMarkup.Engine.Logging;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.System;
using DependencyResolver = LessMarkup.Interfaces.DependencyResolver;

namespace LessMarkup.Engine.Module
{
    public class ModuleProvider : IModuleProvider
    {
        private readonly List<ModuleConfiguration> _moduleConfigurations = new List<ModuleConfiguration>();
        private readonly List<Assembly> _moduleAssemblies = new List<Assembly>();
        private readonly ISpecialFolder _specialFolder = new SpecialFolder();
        private readonly IEngineConfiguration _engineConfiguration = new EngineConfiguration();
        private readonly Dictionary<string, Assembly> _assemblyToFullName = new Dictionary<string, Assembly>();

        class ControllerConfiguration
        {
            public string ModuleType { get; set; }
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

            DiscoverDirectory(currentPath, assemblies);

            var directories = WebConfigurationManager.AppSettings.GetValues("ModuleSearchPath");
            if (directories != null)
            {
                foreach (var directory in directories)
                {
                    if (string.IsNullOrWhiteSpace(directory))
                    {
                        continue;
                    }
                    DiscoverDirectory(directory, assemblies);
                }
            }

            var moduleSearchPath = _engineConfiguration.ModuleSearchPath;

            if (!string.IsNullOrWhiteSpace(moduleSearchPath))
            {
                DiscoverDirectory(moduleSearchPath, assemblies);
            }

            return assemblies;
        }

        private void DiscoverDirectory(string currentPath, HashSet<Assembly> assemblies)
        {
            foreach (var file in new DirectoryInfo(currentPath).GetFiles("*.dll").Where(f => f.FullName.EndsWith(".Module.dll"))
                )
            {
                DiscoverModule(file, assemblies);
            }
        }

        private void DiscoverModule(FileInfo file, HashSet<Assembly> assemblies)
        {
            var assembly = Assembly.LoadFile(file.FullName);

            var codeBase = assembly.CodeBase.ToLower();

            if (_moduleAssemblies.Any(a => a.CodeBase.ToLower() == codeBase))
            {
                return;
            }

            var initializerType =
                assembly.GetTypes().FirstOrDefault(t => !t.IsAbstract && typeof (IModuleInitializer).IsAssignableFrom(t));

            if (initializerType == null)
            {
                return;
            }

            RegisterModule(assembly, false, initializerType);

            assemblies.Add(assembly);

            var path = new Uri(assembly.CodeBase).LocalPath;

            var pos = path.LastIndexOf('\\');

            if (pos > 0)
            {
                path = path.Substring(0, pos);
            }

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                var referencePath = Path.Combine(path, reference.Name + ".dll");
                Assembly referencedAssembly;
                if (File.Exists(referencePath))
                {
                    referencedAssembly = Assembly.LoadFile(referencePath);
                }
                else
                {
                    referencedAssembly = Assembly.Load(reference);
                }
                _assemblyToFullName[referencedAssembly.FullName] = referencedAssembly;
                assemblies.Add(referencedAssembly);
            }
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
                foreach (var module in domainModel.GetCollection<Interfaces.Data.Module>().Where(m => !m.Removed))
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
                        module.ModuleType = reference.ModuleType;
                    }
                }

                foreach (var source in _moduleConfigurations.Where(m => !existingModules.Contains(m)))
                {
                    var module = new Interfaces.Data.Module
                    {
                        Enabled = true,
                        Name = source.Name,
                        Path = source.Path,
                        Removed = false,
                        System = source.System,
                        ModuleType = source.ModuleType
                    };

                    domainModel.GetCollection<Interfaces.Data.Module>().Add(module);
                    domainModel.SaveChanges();
                }

                domainModel.SaveChanges();
            }
        }

        public void InitializeModules()
        {
            var modulesToRemove = new List<ModuleConfiguration>();

            var moduleIntegration = DependencyResolver.Resolve<IModuleIntegration>() as ModuleIntegration;

            if (moduleIntegration == null)
            {
                throw new NullReferenceException("Cannot read module integration");
            }

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

                moduleIntegration.RegisteringModuleType = module.ModuleType;
                try
                {
                    initializer.Initialize();
                }
                finally
                {
                    moduleIntegration.RegisteringModuleType = null;
                }

                module.Namespace = initializer.DefaultNamespace;
                module.Name = initializer.Name;
                module.Initializer = initializer;
                module.ModuleType = initializer.ModuleType;

                foreach (var type in module.Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof (Controller))))
                {
                    var name = type.Name;

                    if (name.EndsWith("Controller"))
                    {
                        name = name.Remove(name.Length - "Controller".Length);
                    }

                    var controllerConfiguration = new ControllerConfiguration
                    {
                        ModuleType = initializer.ModuleType,
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
            var moduleIntegration = DependencyResolver.Resolve<IModuleIntegration>() as ModuleIntegration;

            if (moduleIntegration == null)
            {
                throw new NullReferenceException("Cannot read module integration");
            }
            
            foreach (var module in _moduleConfigurations)
            {
                if (module.Initializer != null)
                {
                    this.LogDebug("Initializing module " + module.Name);
                    moduleIntegration.RegisteringModuleType = module.ModuleType;
                    try
                    {
                        module.Initializer.InitializeDatabase();
                    }
                    finally
                    {
                        moduleIntegration.RegisteringModuleType = null;
                    }
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

        public string GetControllerModuleType(Type controllerType)
        {
            ControllerConfiguration ret;
            if (!_controllersByType.TryGetValue(controllerType, out ret))
            {
                return null;
            }
            return ret.ModuleType;
        }

        public string GetControllerModuleType(string controllerName)
        {
            ControllerConfiguration ret;
            if (!_controllersByName.TryGetValue(controllerName, out ret))
            {
                return null;
            }
            return ret.ModuleType;
        }

        public Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            Assembly ret;
            if (_assemblyToFullName.TryGetValue(args.Name, out ret))
            {
                return ret;
            }
            return null;
        }

        public void RegisterModule(Assembly moduleAssembly, bool systemModule, Type initializerType)
        {
            var path = new Uri(moduleAssembly.CodeBase).LocalPath;

            var pos = path.LastIndexOf('\\');

            if (pos > 0)
            {
                path = path.Substring(0, pos);
            }

            foreach (var reference in moduleAssembly.GetReferencedAssemblies())
            {
                var assemblyPath = Path.Combine(path, reference.Name + ".dll");
                Assembly assembly;
                if (File.Exists(assemblyPath))
                {
                    assembly = Assembly.LoadFile(assemblyPath);
                }
                else
                {
                    assembly = Assembly.Load(reference);
                }
                _assemblyToFullName[assembly.FullName] = assembly;
            }
            
            _moduleAssemblies.Add(moduleAssembly);

            var configuration = new ModuleConfiguration(moduleAssembly, systemModule, initializerType);

            _moduleConfigurations.Add(configuration);
        }
    }
}
