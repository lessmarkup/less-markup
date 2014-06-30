/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Reflection;

namespace LessMarkup.Interfaces.Module
{
    public class ModuleConfiguration
    {
        private readonly Assembly _assembly;
        private readonly string _path;
        private readonly bool _system;
        private readonly Type _initializerType;

        public ModuleConfiguration(Assembly assembly, bool system, Type initializerType)
        {
            _assembly = assembly;
            _path = new Uri(assembly.CodeBase).LocalPath;
            _system = system;
            _initializerType = initializerType;
        }

        public Type InitializerType { get { return _initializerType; } }

        public bool System { get { return _system; } }

        public IModuleInitializer Initializer { get; set; }

        public ModuleType ModuleType { get; set; }

        public Assembly Assembly { get { return _assembly; } }

        public string Namespace { get; set; }

        public string Name { get; set; }

        public string Path { get { return _path; } }
    }
}
