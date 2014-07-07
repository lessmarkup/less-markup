/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Engine.Build.Data;
using LessMarkup.Engine.Build.View;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.System;

namespace LessMarkup.Engine.Build
{
    class BuildEngine : IBuildEngine
    {
        private readonly DataBuilder _dataBuilder;
        private readonly ViewBuilder _viewBuilder;

        public BuildEngine(ISpecialFolder specialFolder, IModuleProvider moduleProvider)
        {
            _dataBuilder = new DataBuilder(specialFolder);
            _viewBuilder = new ViewBuilder(specialFolder, moduleProvider);
        }

        public bool IsActive
        {
            get
            {
                return _dataBuilder.IsActive && _viewBuilder.IsActive;
            }
        }

        public bool IsRecent
        {
            get
            {
                return _dataBuilder.IsRecent && _viewBuilder.IsRecent;
            }
        }

        public void Build()
        {
            _dataBuilder.Build();
            _viewBuilder.Build();
        }

        public void Activate()
        {
            _dataBuilder.Activate();
            _viewBuilder.Activate();
        }

        public void RefreshTemplateList(IDomainModelProvider domainModelProvider)
        {
            _viewBuilder.ImportTemplates(domainModelProvider);
        }

        public DateTime LastBuildTime
        {
            get
            {
                var viewTime = _viewBuilder.LastBuildTime;
                var dataTime = _dataBuilder.LastBuildTime;
                if (viewTime < dataTime)
                {
                    return viewTime;
                }
                return dataTime;
            }
        }
    }
}
