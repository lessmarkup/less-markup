/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;
using System.Linq;
using LessMarkup.DataFramework;
using LessMarkup.Engine.Helpers;
using LessMarkup.Engine.Language;
using LessMarkup.Framework.NodeHandlers;
using LessMarkup.Interfaces;
using LessMarkup.Interfaces.RecordModel;
using LessMarkup.UserInterface.Model.RecordModel;

namespace LessMarkup.UserInterface.NodeHandlers.Common
{
    public abstract class DialogNodeHandler<T> : AbstractNodeHandler
    {
        private readonly HashSet<string> _scripts = new HashSet<string>();

        protected abstract T LoadObject();
        protected abstract string SaveObject(T changedObject);

        protected virtual string ApplyCaption { get { return LanguageHelper.GetText(Constants.ModuleType.MainModule, MainModuleTextIds.ApplyButton); } }

        protected override object GetViewData()
        {
            var definitionModel = DependencyResolver.Resolve<InputFormDefinitionModel>();
            definitionModel.Initialize(typeof (T));

            foreach (var field in definitionModel.Fields)
            {
                switch (field.Type)
                {
                    case InputFieldType.CodeText:
                        _scripts.Add("lib/codemirror/codemirror");
                        _scripts.Add("lib/codemirror/plugins/css");
                        _scripts.Add("lib/codemirror/plugins/css-hint");
                        _scripts.Add("lib/codemirror/plugins/dialog");
                        _scripts.Add("lib/codemirror/plugins/anyword-hint");
                        _scripts.Add("lib/codemirror/plugins/brace-fold");
                        _scripts.Add("lib/codemirror/plugins/closebrackets");
                        _scripts.Add("lib/codemirror/plugins/closetag");
                        _scripts.Add("lib/codemirror/plugins/colorize");
                        _scripts.Add("lib/codemirror/plugins/comment");
                        _scripts.Add("lib/codemirror/plugins/comment-fold");
                        _scripts.Add("lib/codemirror/plugins/continuecomment");
                        _scripts.Add("lib/codemirror/plugins/foldcode");
                        _scripts.Add("lib/codemirror/plugins/fullscreen");
                        _scripts.Add("lib/codemirror/plugins/html-hint");
                        _scripts.Add("lib/codemirror/plugins/htmlembedded");
                        _scripts.Add("lib/codemirror/plugins/htmlmixed");
                        _scripts.Add("lib/codemirror/plugins/indent-fold");
                        _scripts.Add("lib/codemirror/plugins/javascript");
                        _scripts.Add("lib/codemirror/plugins/javascript-hint");
                        _scripts.Add("lib/codemirror/plugins/mark-selection");
                        _scripts.Add("lib/codemirror/plugins/markdown-fold");
                        _scripts.Add("lib/codemirror/plugins/match-highlighter");
                        _scripts.Add("lib/codemirror/plugins/matchbrackets");
                        _scripts.Add("lib/codemirror/plugins/matchtags");
                        _scripts.Add("lib/codemirror/plugins/placeholder");
                        _scripts.Add("lib/codemirror/plugins/rulers");
                        _scripts.Add("lib/codemirror/plugins/scrollpastend");
                        _scripts.Add("lib/codemirror/plugins/search");
                        _scripts.Add("lib/codemirror/plugins/searchcursor");
                        _scripts.Add("lib/codemirror/plugins/xml");
                        _scripts.Add("lib/codemirror/plugins/xml-fold");
                        _scripts.Add("lib/codemirror/plugins/xml-hint");
                        _scripts.Add("lib/codemirror/ui-codemirror");
                        break;
                    case InputFieldType.RichText:
                        _scripts.Add("lib/tinymce/tinymce");
                        _scripts.Add("lib/tinymce/config");
                        _scripts.Add("lib/tinymce/tinymce-angular");
                        break;
                }
            }

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
            get { return _scripts.ToArray(); }
        }
    }
}
