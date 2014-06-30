/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Data.Entity;
using LessMarkup.Articles.DataObjects;

namespace LessMarkup.Articles.DataAccess
{
    public class DomainModel : DbContext
    {
        public DomainModel() : base("Server")
        {
            Configuration.LazyLoadingEnabled = true;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            new ModelCreate().OnModelCreating(modelBuilder);
        }

        public DbSet<Article> Articles { get; set; }
    }
}
