using LessMarkup.DataObjects.Common;
using LessMarkup.DataObjects.Security;
using LessMarkup.DataObjects.Structure;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Migrations
{
    public class Initial : Migration
    {
        public override string Id
        {
            get { return "20141123_172800"; }
        }

        public override void Migrate(IMigrator migrator)
        {
            migrator.CreateTable<User>();
            migrator.CreateTable<Currency>();
            migrator.CreateTable<EntityChangeHistory>();
            migrator.CreateTable<FailedLoginHistory>();
            migrator.CreateTable<File>();
            migrator.CreateTable<Image>();
            migrator.CreateTable<Language>();
            migrator.CreateTable<Menu>();
            migrator.CreateTable<Module>();
            migrator.CreateTable<NodeAccess>();
            migrator.CreateTable<Node>();
            migrator.CreateTable<NodeUserData>();
            migrator.CreateTable<SiteCustomization>();
            migrator.CreateTable<Smile>();
            migrator.CreateTable<SuccessfulLoginHistory>();
            migrator.CreateTable<TestMail>();
            migrator.CreateTable<Translation>();
            migrator.CreateTable<UserAddress>();
            migrator.CreateTable<UserBlockHistory>();
            migrator.CreateTable<UserGroupMembership>();
            migrator.CreateTable<UserGroup>();
            migrator.CreateTable<UserLoginIpAddress>();
            migrator.CreateTable<UserPropertyDefinition>();
            migrator.CreateTable<ViewHistory>();
            migrator.CreateTable<SiteProperties>();
            migrator.AddDependency<Translation, Language>();
            migrator.AddDependency<NodeUserData, Node>();
            migrator.AddDependency<NodeUserData, User>();
            migrator.AddDependency<NodeAccess, Node>();
            migrator.AddDependency<UserBlockHistory, User>();
            migrator.AddDependency<UserBlockHistory, User>("BlockedByUserId");
            migrator.AddDependency<UserGroupMembership, User>();
            migrator.AddDependency<UserGroupMembership, UserGroup>();
            migrator.AddDependency<UserLoginIpAddress, User>();
            migrator.AddDependency<ViewHistory, User>();
        }
    }
}
