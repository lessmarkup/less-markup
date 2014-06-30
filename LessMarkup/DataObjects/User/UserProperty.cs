/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.User
{
    public class UserProperty : SiteDataObject
    {
        public long UserPropertyId { get; set; }
        public UserPropertyDefinition Definition { get; set; }
        [ForeignKey("Definition")]
        public long DefinitionId { get; set; }
        public LessMarkup.DataObjects.User.User User { get; set; }
        [ForeignKey("User")]
        public long UserId { get; set; }
        public string Value { get; set; }
    }
}
