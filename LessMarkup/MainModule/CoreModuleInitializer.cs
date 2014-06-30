/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Framework.Module;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.MainModule
{
    public class CoreModuleInitializer : BaseModuleInitializer
    {
        public override string Name { get { return "Core"; } }

        public override ModuleType Type
        {
            get { return ModuleType.Core; }
        }

        public override Type[] ModelTypes
        {
            get { return null; }
        }
    }
}
