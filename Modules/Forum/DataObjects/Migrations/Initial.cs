using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Forum.DataObjects.Migrations
{
    public class Initial : Migration
    {
        public override void Migrate(IMigrator migrator)
        {
            migrator.CreateTable<Thread>();
            migrator.CreateTable<Post>();
            migrator.CreateTable<PostAttachment>();
            migrator.CreateTable<PostHistory>();
            migrator.CreateTable<ThreadView>();

            migrator.AddDependency<Thread, User>("AuthorId");
            migrator.AddDependency<Post, Thread>();
            migrator.AddDependency<Post, User>();
            migrator.AddDependency<PostAttachment, Post>();
            migrator.AddDependency<PostHistory, Post>();
            migrator.AddDependency<PostHistory, User>();
            migrator.AddDependency<ThreadView, Thread>();
            migrator.AddDependency<ThreadView, User>();
        }

        public override string Id
        {
            get { return "20141123_211800"; }
        }
    }
}
