/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.DataFramework;
using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Interfaces.Cache;
using LessMarkup.MainModule.Model;
using LessMarkup.UserInterface.NodeHandlers.Common;

namespace LessMarkup.MainModule.NodeHandlers
{
    public class ContactFormNodeHandler : DialogNodeHandler<SendContactModel>
    {
        public ContactFormNodeHandler(IDataCache dataCache) : base(dataCache)
        {
        }

        protected override SendContactModel LoadObject()
        {
            var contactFormSettings = GetSettings<ContactFormModel>();

            var result = Interfaces.DependencyResolver.Resolve<SendContactModel>();

            if (contactFormSettings != null)
            {
                result.UserEmail = contactFormSettings.ContactEmail;
                result.Subject = contactFormSettings.ContactSubject;
                result.Caption = contactFormSettings.Caption;
            }

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

        protected override Type SettingsModel
        {
            get { return typeof(ContactFormModel); }
        }
    }
}
