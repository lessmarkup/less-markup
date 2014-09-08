/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Structure
{
    public class NodeUserData : SiteDataObject
    {
        [ForeignKey("Node")]
        public long NodeId { get; set; }
        public Node Node { get; set; }
        [ForeignKey("User")]
        public long UserId { get; set; }
        public User User { get; set; }
        public string Settings { get; set; }
    }
}
