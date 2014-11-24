﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Framework;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;
using LessMarkup.MainModule.Model;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.MainModule.NodeHandlers
{
    [ConfigurationHandler(MainModuleTextIds.Smiles)]
    public class SmileNodeHandler : RecordListNodeHandler<SmileModel>
    {
        public SmileNodeHandler(ILightDomainModelProvider domainModelProvider, IDataCache dataCache) : base(domainModelProvider, dataCache)
        {
        }
    }
}
