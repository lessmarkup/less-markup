/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.UserInterface.Model.Common
{
    [RecordModel(TitleTextId = UserInterfaceTextIds.FlatPageSettings)]
    public class FlatPageSettingsModel
    {
        [InputField(InputFieldType.CheckBox, UserInterfaceTextIds.LoadOnShow, DefaultValue = false)]
        public bool LoadOnShow { get; set; }

        [InputField(InputFieldType.Select, UserInterfaceTextIds.Position, DefaultValue = FlatPagePosition.Right)]
        public FlatPagePosition Position { get; set; }
    }
}
