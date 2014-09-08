/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using LessMarkup.DataFramework;
using LessMarkup.Framework;
using LessMarkup.Framework.Helpers;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.Cache;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.UserInterface.Model.RecordModel;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public abstract class DialogNodeHandler<T> : AbstractNodeHandler
    {
        protected abstract T LoadObject();
        protected abstract string SaveObject(T changedObject);

        private readonly InputFormDefinitionModel _definitionModel;
        private readonly IDataCache _dataCache;

        protected DialogNodeHandler(IDataCache dataCache)
        {
            _dataCache = dataCache;

            _definitionModel = DependencyResolver.Resolve<InputFormDefinitionModel>();
            _definitionModel.Initialize(typeof(T));

            foreach (var field in _definitionModel.Fields)
            {
                switch (field.Type)
                {
                    case InputFieldType.CodeText:
                        AddScript("lib/codemirror/codemirror");
                        AddScript("lib/codemirror/plugins/css");
                        AddScript("lib/codemirror/plugins/css-hint");
                        AddScript("lib/codemirror/plugins/dialog");
                        AddScript("lib/codemirror/plugins/anyword-hint");
                        AddScript("lib/codemirror/plugins/brace-fold");
                        AddScript("lib/codemirror/plugins/closebrackets");
                        AddScript("lib/codemirror/plugins/closetag");
                        AddScript("lib/codemirror/plugins/colorize");
                        AddScript("lib/codemirror/plugins/comment");
                        AddScript("lib/codemirror/plugins/comment-fold");
                        AddScript("lib/codemirror/plugins/continuecomment");
                        AddScript("lib/codemirror/plugins/foldcode");
                        AddScript("lib/codemirror/plugins/fullscreen");
                        AddScript("lib/codemirror/plugins/html-hint");
                        AddScript("lib/codemirror/plugins/htmlembedded");
                        AddScript("lib/codemirror/plugins/htmlmixed");
                        AddScript("lib/codemirror/plugins/indent-fold");
                        AddScript("lib/codemirror/plugins/javascript");
                        AddScript("lib/codemirror/plugins/javascript-hint");
                        AddScript("lib/codemirror/plugins/mark-selection");
                        AddScript("lib/codemirror/plugins/markdown-fold");
                        AddScript("lib/codemirror/plugins/match-highlighter");
                        AddScript("lib/codemirror/plugins/matchbrackets");
                        AddScript("lib/codemirror/plugins/matchtags");
                        AddScript("lib/codemirror/plugins/placeholder");
                        AddScript("lib/codemirror/plugins/rulers");
                        AddScript("lib/codemirror/plugins/scrollpastend");
                        AddScript("lib/codemirror/plugins/search");
                        AddScript("lib/codemirror/plugins/searchcursor");
                        AddScript("lib/codemirror/plugins/xml");
                        AddScript("lib/codemirror/plugins/xml-fold");
                        AddScript("lib/codemirror/plugins/xml-hint");
                        AddScript("lib/codemirror/ui-codemirror");
                        break;
                    case InputFieldType.RichText:
                        AddScript("lib/ckeditor/ckeditor");
                        AddScript("directives/angular-ckeditor");
                        break;
                }
            }
        }

        protected virtual string ApplyCaption { get { return LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.ApplyButton); } }

        protected override Dictionary<string, object> GetViewData()
        {
            return new Dictionary<string, object>
            {
                { "Definition", _definitionModel },
                { "Object", LoadObject() },
                { "ApplyCaption", ApplyCaption }
            };
        }

        public string Save(T changedObject, string rawChangedObject)
        {
            var model = _dataCache.Get<IRecordModelCache>().GetDefinition<T>();
            model.ValidateInput(changedObject, false, rawChangedObject);
            return SaveObject(changedObject) ?? LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.SuccessfullySaved);
        }

        protected override string ViewType
        {
            get { return "Dialog"; }
        }
    }
}
