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
        private readonly List<string> _scripts = new List<string>(); 
        private readonly List<string> _stylesheets = new List<string>();

        public abstract object GetViewData(long objectId, object settings, object controller);
        public virtual bool HasChildren { get { return false; } }
        public virtual ChildHandlerSettings GetChildHandler(string path)
        {
            return null;
        }

        public virtual bool IsStatic { get { return false; } }

        public string[] Scripts { get { return _scripts.ToArray(); } }
        public string[] Stylesheets { get { return _stylesheets.ToArray(); } }
        public virtual Type SettingsModel { get { return null; } }

        public string TemplateId
        {
            get { return ViewType.ToLower(); }
        }

        public virtual string ViewType
        {
            get { return GetType().Name; }
        }

        protected void AddScript(string script)
        {
            _scripts.Add(script);
        }

        protected void AddStylesheet(string stylesheet)
        {
            _stylesheets.Add(stylesheet);
        }
    }
}
