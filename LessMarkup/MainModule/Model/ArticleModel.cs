/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using LessMarkup.Engine.Language;
using LessMarkup.Framework;
using LessMarkup.Interfaces.RecordModel;

namespace LessMarkup.MainModule.Model
{
    [RecordModel(TitleTextId = MainModuleTextIds.EditArticle)]
    public class ArticleModel
    {
        [InputField(InputFieldType.RichText, MainModuleTextIds.Body, Required = true)]
        public string Body { get; set; }
    }
}
