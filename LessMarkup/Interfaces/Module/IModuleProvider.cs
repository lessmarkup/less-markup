/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Reflection;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Interfaces.Module
{
    public interface IModuleProvider
    {
        IEnumerable<ModuleConfiguration> Modules { get; }
        void RegisterModule(Assembly moduleAssembly, bool systemModule, Type initializerType);
        IEnumerable<Assembly> DiscoverAndRegisterModules();
        void UpdateModuleDatabase(IDomainModelProvider domainModelProvider);
        void InitializeModules();
        void InitializeModulesDatabase();
        Type GetControllerType(string controllerName);
        string GetControllerModuleType(Type controllerType);
        string GetControllerModuleType(string controllerName);
    }
}
