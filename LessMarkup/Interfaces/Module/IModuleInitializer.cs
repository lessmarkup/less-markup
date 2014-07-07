/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.Module
{
    public interface IModuleInitializer
    {
        void Initialize();
        void InitializeDatabase();
        string DefaultNamespace { get; }
        string Name { get; }
        string ModuleType { get; }
        Type[] ModelTypes { get; }
    }
}
