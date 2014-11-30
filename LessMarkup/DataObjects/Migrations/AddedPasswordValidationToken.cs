using LessMarkup.DataObjects.Security;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataObjects.Migrations
{
    public class AddedPasswordValidationToken : Migration
    {
        public override string Id
        {
            get { return "20141128_171500"; }
        }

        public override void Migrate(IMigrator migrator)
        {
            migrator.UpdateTable<User>();
        }
    }
}
