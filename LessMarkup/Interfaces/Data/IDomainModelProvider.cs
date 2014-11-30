namespace LessMarkup.Interfaces.Data
{
    public interface IDomainModelProvider
    {
        IDomainModel Create();
        IDomainModel CreateWithTransaction();
    }
}
