namespace LessMarkup.Interfaces.Data
{
    public interface ILightDomainModelProvider
    {
        ILightDomainModel Create();
        ILightDomainModel CreateWithTransaction();
    }
}
