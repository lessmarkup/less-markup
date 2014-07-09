/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.ComponentModel.DataAnnotations.Schema;
using LessMarkup.DataObjects.User;
using LessMarkup.Interfaces.Data;
using LessMarkup.Interfaces.Structure;

namespace LessMarkup.DataObjects.Structure
{
    public class NodeAccess : SiteDataObject
    {
        public long NodeAccessId { get; set; }
        [ForeignKey("Node")]
        public long NodeId { get; set; }
        public Node Node { get; set; }
        public NodeAccessType AccessType { get; set; }
        [ForeignKey("User")]
        public long? UserId { get; set; }
        public User.User User { get; set; }
        [ForeignKey("Group")]
        public long? GroupId { get; set; }
        public UserGroup Group { get; set; }
    }
}
