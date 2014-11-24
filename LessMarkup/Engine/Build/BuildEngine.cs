/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Engine.Build.View;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Build
{
    class BuildEngine : IBuildEngine
    {
        private readonly ViewBuilder _viewBuilder;

        public BuildEngine(ISpecialFolder specialFolder, IModuleProvider moduleProvider)
        {
            _viewBuilder = new ViewBuilder(specialFolder, moduleProvider);
        }

        public bool IsActive
        {
            get
            {
                return _viewBuilder.IsActive;
            }
        }

        public bool IsRecent
        {
            get
            {
                return _viewBuilder.IsRecent;
            }
        }

        public void Build()
        {
            _viewBuilder.Build();
        }

        public void Activate()
        {
            _viewBuilder.Activate();
        }

        public void RefreshTemplateList()
        {
            _viewBuilder.ImportTemplates();
        }

        public DateTime LastBuildTime
        {
            get
            {
                var viewTime = _viewBuilder.LastBuildTime;
                return viewTime;
            }
        }
    }
}
