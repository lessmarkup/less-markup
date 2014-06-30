/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.ComponentModel.DataAnnotations;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Text;

namespace LessMarkup.Articles.DataObjects
{
    [Entity(EntityType.Article)]
    public class Article : SiteDataObject
    {
        [Key]
        public long ArticleId { get; set; }
        [TextSearch]
        public string Title { get; set; }
        [TextSearch]
        public string Body { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Updated { get; set; }
        public long AuthorId { get; set; }
        public string MenuId { get; set; }
        public int? Order { get; set; }
    }
}
