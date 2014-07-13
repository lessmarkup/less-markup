/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.DataFramework;
using LessMarkup.Engine.Helpers;
using LessMarkup.Engine.Language;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces;
using LessMarkup.UserInterface.Model.RecordModel;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public abstract class DialogNodeHandler<T> : AbstractNodeHandler
    {
        protected abstract T LoadObject();
        protected abstract string SaveObject(T changedObject);

        protected virtual string ApplyCaption { get { return LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.ApplyButton); } }

        protected override object GetViewData()
        {
            var definitionModel = DependencyResolver.Resolve<InputFormDefinitionModel>();
            definitionModel.Initialize(typeof (T));

            return new
            {
                Definition = definitionModel,
                Object = LoadObject(),
                ApplyCaption
            };
        }

        public string Save(T changedObject)
        {
            return SaveObject(changedObject) ?? LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.SuccessfullySaved);
        }

        protected override string ViewType
        {
            get { return "Dialog"; }
        }

        protected override string[] Scripts
        {
            get { return new []{ "controllers/dialog" }; }
        }
    }
}
