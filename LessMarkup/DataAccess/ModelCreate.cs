/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Data.Entity;
using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataAccess
{
    public class ModelCreate : IModelCreate
    {
        public void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasMany(u => u.Groups).WithRequired(m => m.User);
            modelBuilder.Entity<User>().HasOptional(u => u.Site).WithMany().WillCascadeOnDelete(false);
            modelBuilder.Entity<Site>().HasMany(s => s.Modules).WithRequired(m => m.Site);
        }
    }
}
