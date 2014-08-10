/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.Framework.NodeHandlers
{
    public abstract class AbstractNodeHandler : INodeHandler
    {
        private long? _objectId;
        private string _path;
        private object _settings;
        private NodeAccessType _accessType;
        private List<string> _scripts = new List<string>(); 
        private List<string> _stylesheets = new List<string>();

        public string Path { get { return _path; } }

        public long? ObjectId { get { return _objectId; } }

        public NodeAccessType AccessType { get { return _accessType; } }

        public bool HasManageAccess
        {
            get { return AccessType == NodeAccessType.Manage; }
        }

        public bool HasWriteAccess
        {
            get { return AccessType == NodeAccessType.Manage || AccessType == NodeAccessType.Write; }
        }

        public bool HasReadAccess
        {
            get { return HasWriteAccess || AccessType == NodeAccessType.Read; }
        }

        protected T GetSettings<T>()
        {
            return (T) _settings;
        }

        protected void AddScript(string script)
        {
            _scripts.Add(script);
        }

        protected void AddStylesheet(string stylesheet)
        {
            _stylesheets.Add(stylesheet);
        }

        #region INodeHandler Implementation

        object INodeHandler.GetViewData()
        {
            return GetViewData();
        }

        object INodeHandler.Initialize(long? objectId, object settings, object controller, string path, NodeAccessType accessType)
        {
            _objectId = objectId;
            _path = path;
            _settings = settings;
            _accessType = accessType;

            return Initialize(controller);
        }

        bool INodeHandler.HasChildren { get { return HasChildren; } }

        ChildHandlerSettings INodeHandler.GetChildHandler(string path)
        {
            return GetChildHandler(path);
        }

        bool INodeHandler.IsStatic { get { return IsStatic; } }

        List<string> INodeHandler.Scripts { get { return _scripts; } }
        List<string> INodeHandler.Stylesheets { get { return _stylesheets; } }

        Type INodeHandler.SettingsModel { get { return SettingsModel; } }

        string INodeHandler.TemplateId { get { return TemplateId; } }

        string INodeHandler.ViewType { get { return ViewType; } }

        #endregion

        protected virtual object GetViewData()
        {
            return null;
        }

        protected virtual object Initialize(object controller)
        {
            return null;
        }

        protected virtual bool HasChildren {get { return false; }}

        protected virtual ChildHandlerSettings GetChildHandler(string path)
        {
            return null;
        }

        protected virtual bool IsStatic { get { return false; } }

        protected virtual Type SettingsModel { get { return null; } }

        protected virtual string TemplateId
        {
            get { return ViewType.ToLower(); }
        }

        protected virtual string ViewType
        {
            get { return GetType().Name; }
        }
    }
}
