/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.Module
{
    public abstract class BaseModuleInitializer : IModuleInitializer
    {
        public virtual void Initialize()
        {
        }

        public virtual void InitializeDatabase()
        {
        }

        public virtual string DefaultNamespace
        {
            get
            {
                return GetType().Namespace;
            }
        }

        public abstract string Name { get; }
        public abstract string ModuleType { get; }
        public abstract Type[] ModelTypes { get; }
    }
}
