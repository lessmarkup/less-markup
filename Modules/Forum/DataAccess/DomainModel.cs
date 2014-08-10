/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Data.Entity;
using LessMarkup.Forum.DataObjects;

namespace LessMarkup.Forum.DataAccess
{
    public class DomainModel : DbContext
    {
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            new ModelCreate().OnModelCreating(modelBuilder);
        }

        public DbSet<Thread> ForumThreads { get; set; }

        public DbSet<Post> ForumPosts { get; set; }

        public DbSet<PostAttachment> ForumPostAttachments { get; set; }

        public DbSet<PostHistory> ForumPostHistories { get; set; }

        public DbSet<ThreadView> ForumThreadViews { get; set; }
    }
}
