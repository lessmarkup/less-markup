/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Data.Entity;
using LessMarkup.DataObjects.Common;
using LessMarkup.DataObjects.Gallery;
using LessMarkup.DataObjects.Security;
using LessMarkup.DataObjects.Statistics;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataAccess
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

        public DbSet<User> Users { get; set; }
        public DbSet<EntityChangeHistory> EntityChangeHistories { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<UserGroupMembership> UserGroupMemberships { get; set; }
        public DbSet<FailedLoginHistory> FailedLoginHistories { get; set; }
        public DbSet<SuccessfulLoginHistory> SuccessfulLoginHistories { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<TestMail> TestMails { get; set; }
        public DbSet<Smile> Smiles { get; set; }
        public DbSet<UserLoginIpAddress> UserLoginIpAddresses { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Translation> Translations { get; set; }
        public DbSet<Site> Sites { get; set; }
        public DbSet<SiteModule> SiteModules { get; set; }
        public DbSet<Module> Modules { get; set; }
        public DbSet<SiteCustomization> SiteTemplates { get; set; }
        public DbSet<Gallery> Galleries { get; set; }
        public DbSet<GalleryImage> GalleryImages { get; set; }
        public DbSet<Currency> Currencies { get; set; }
        public DbSet<ViewHistory> ViewHistories { get; set; }
        public DbSet<AddressCountry> AddressCountries { get; set; }
        public DbSet<AddressHistory> AddressHistories { get; set; }
        public DbSet<AddressToCountry> AddressToCountries { get; set; }
        public DbSet<DaySummaryHistory> DaySummaryHistories { get; set; }
        public DbSet<Node> Nodes { get; set; }
        public DbSet<NodeAccess> NodeAccesses { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<UserPropertyDefinition> UserPropertyDefinitions { get; set; }
    }
}
