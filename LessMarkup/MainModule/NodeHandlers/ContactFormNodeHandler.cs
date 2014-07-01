/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Framework.Helpers;
using LessMarkup.Framework.Language;
using LessMarkup.Interfaces.Module;
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

        protected override string SaveObject(SendContactModel changedObject)
        {
            changedObject.Submit();

            return LanguageHelper.GetText(ModuleType.MainModule, MainModuleTextIds.ContactFormSent);
        }
    }
}
