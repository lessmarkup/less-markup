/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.DataFramework;
using LessMarkup.Engine.Helpers;
using LessMarkup.Engine.Language;
using LessMarkup.MainModule.Model;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.MainModule.NodeHandlers
{
    public class ContactFormNodeHandler : DialogNodeHandler<SendContactModel>
    {
        protected override SendContactModel LoadObject(object settings)
        {
            var contactFormSettings = (ContactFormModel) settings;

            var result = Interfaces.DependencyResolver.Resolve<SendContactModel>();
            result.UserEmail = contactFormSettings.ContactEmail;
            result.Subject = contactFormSettings.ContactSubject;

            return result;
        }

        protected override string ApplyCaption
        {
            get { return LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.SendMessage); }
        }

        protected override string SaveObject(SendContactModel changedObject)
        {
            changedObject.Submit();

            return LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.ContactFormSent);
        }

        public override Type SettingsModel
        {
            get { return typeof(ContactFormModel); }
        }
    }
}
