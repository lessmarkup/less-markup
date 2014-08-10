﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;

namespace LessMarkup.Interfaces.Structure
{
    public interface INodeHandler
    {
        object Initialize(long? objectId, object settings, object controller, string path, NodeAccessType accessType);
        object GetViewData();
        bool HasChildren { get; }
        bool IsStatic { get; }
        ChildHandlerSettings GetChildHandler(string path);
        List<string> Stylesheets { get; }
        Type SettingsModel { get; }
        string TemplateId { get; }
        string ViewType { get; }
        List<string> Scripts { get; }
        NodeAccessType AccessType { get; }
    }
}
