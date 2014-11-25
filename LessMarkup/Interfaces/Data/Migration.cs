namespace LessMarkup.Interfaces.Data
{
    public abstract class Migration
    {
        public abstract string Id { get; }
        public abstract void Migrate(IMigrator migrator);
    }
}
