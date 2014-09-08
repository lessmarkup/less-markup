/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.UserInterface.Model.User
{
    [RecordModel(TitleTextId = UserInterfaceTextIds.ChangePassword)]
    public class ChangePasswordModel
    {
        [InputField(InputFieldType.PasswordRepeat, UserInterfaceTextIds.Password)]
        public string Password { get; set; }
    }
}
