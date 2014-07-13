/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.Structure
{
    public interface INodeHandler
    {
        object Initialize(long? objectId, object settings, object controller, string path, NodeAccessType accessType);
        object GetViewData();
        bool HasChildren { get; }
        bool IsStatic { get; }
        ChildHandlerSettings GetChildHandler(string path);
        string[] Stylesheets { get; }
        Type SettingsModel { get; }
        string TemplateId { get; }
        string ViewType { get; }
        string[] Scripts { get; }
    }
}
