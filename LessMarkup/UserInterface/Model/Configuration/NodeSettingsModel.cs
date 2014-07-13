/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.UserInterface.NodeHandlers.Configuration;

namespace LessMarkup.UserInterface.Model.Configuration
{
    [RecordModel(TitleTextId = UserInterfaceTextIds.NodeSettings)]
    public class NodeSettingsModel : IInputSource
    {
        private readonly IModuleIntegration _moduleIntegration;
        private readonly IModuleProvider _moduleProvider;

        public NodeSettingsModel(IModuleIntegration moduleIntegration, IModuleProvider moduleProvider)
        {
            _moduleIntegration = moduleIntegration;
            _moduleProvider = moduleProvider;
        }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.Title, Required = true)]
        public string Title { get; set; }

        [InputField(InputFieldType.Select, UserInterfaceTextIds.Handler, Required = true)]
        public string HandlerId { get; set; }

        public object Settings { get; set; }

        public string SettingsModelId { get; set; }
        public long NodeId { get; set; }

        public int Level { get; set; }

        public bool Customizable { get; set; }

        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.Enabled, DefaultValue = true)]
        public bool Enabled { get; set; }

        [InputField(InputFieldType.Text, UserInterfaceTextIds.Path, Required = true)]
        public string Path { get; set; }

        public int Order { get; set; }

        public string RoleText { get; set; }

        public List<EnumSource> GetEnumValues(string fieldName)
        {
            switch (fieldName)
            {
                case "HandlerId":
                {
                    var modules = _moduleProvider.Modules.Select(m => m.ModuleType).ToList();
                    return
                        _moduleIntegration.GetNodeHandlers()
                            .Select(id => new {Id = id, Handler = _moduleIntegration.GetNodeHandler(id)})
                            .Where(h => modules.Contains(h.Handler.Item2))
                            .Select(h => new EnumSource
                            {
                                Value = h.Id,
                                Text = NodeListNodeHandler.GetHandlerName(h.Handler.Item1, h.Handler.Item2)
                            }).ToList();
                }
                default:
                    throw new ArgumentOutOfRangeException("fieldName");
            }
        }
    }
}
