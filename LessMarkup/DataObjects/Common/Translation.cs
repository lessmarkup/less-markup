/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.DataFramework.DataObjects;

namespace LessMarkup.DataObjects.Common
{
    public class Translation : SiteDataObject
    {
        public long TranslationId { get; set; }
        [ForeignKey("Language")]
        public long LanguageId { get; set; }
        public Language Language { get; set; }
        public string Reference { get; set; }
        public string OriginalText { get; set; }
        public string Text { get; set; }
    }
}
