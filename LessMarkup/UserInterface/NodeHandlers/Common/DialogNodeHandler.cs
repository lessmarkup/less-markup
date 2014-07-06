/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Engine.Helpers;
using LessMarkup.Engine.Language;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Module;
using LessMarkup.Interfaces.Structure;
using LessMarkup.UserInterface.Model.RecordModel;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public abstract class DialogNodeHandler<T> : AbstractNodeHandler
    {
        protected abstract T LoadObject(object settings);
        protected abstract string SaveObject(T changedObject);

        protected virtual string ApplyCaption { get { return LanguageHelper.GetText(ModuleType.MainModule, MainModuleTextIds.ApplyButton); } }

        public override object GetViewData(long objectId, object settings, object controller)
        {
            var definitionModel = DependencyResolver.Resolve<InputFormDefinitionModel>();
            definitionModel.Initialize(typeof (T));

            return new
            {
                Definition = definitionModel,
                Object = LoadObject(settings),
                ApplyCaption
            };
        }

        public string Save(T changedObject)
        {
            return SaveObject(changedObject) ?? LanguageHelper.GetText(ModuleType.MainModule, MainModuleTextIds.SuccessfullySaved);
        }

        public override string ViewType
        {
            get { return "Dialog"; }
        }
    }
}
