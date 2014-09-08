/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.DataFramework;
using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;

namespace LessMarkup.UserInterface.Exceptions
{
    public class UnknownActionException : Exception
    {
        public UnknownActionException() : base(LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.UnknownCommand))
        {
        }
    }
}
