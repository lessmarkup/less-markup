/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Framework.Helpers;
using LessMarkup.Framework.Language;
using LessMarkup.Interfaces.Module;

namespace LessMarkup.UserInterface.Exceptions
{
    public class UnknownActionException : Exception
    {
        public UnknownActionException() : base(LanguageHelper.GetText(ModuleType.MainModule, MainModuleTextIds.UnknownCommand))
        {
        }
    }
}
