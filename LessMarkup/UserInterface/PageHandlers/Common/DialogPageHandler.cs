/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.DataFramework;
using LessMarkup.UserInterface.Model.RecordModel;

namespace LessMarkup.UserInterface.PageHandlers.Common
{
    public abstract class DialogPageHandler<T> : AbstractPageHandler
    {
        protected abstract T LoadObject();
        protected abstract void SaveObject(T changedObject);

        public override object GetViewData(long objectId, Dictionary<string, string> settings)
        {
            var definitionModel = DependencyResolver.Resolve<InputFormDefinitionModel>();
            definitionModel.Initialize(typeof (T));

            return new
            {
                Definition = definitionModel,
                Object = LoadObject()
            };
        }

        public void Save(T changedObject)
        {
            SaveObject(changedObject);
        }

        public override string ViewType
        {
            get { return "Dialog"; }
        }
    }
}
