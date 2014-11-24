/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.System
{
    public interface IBuildEngine
    {
        void Build();
        void Activate();
        bool IsActive { get; }
        bool IsRecent { get; }
        void RefreshTemplateList();
        DateTime LastBuildTime { get; }
    }
}
