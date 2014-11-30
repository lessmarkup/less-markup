using System;

namespace LessMarkup.Interfaces.Data
{
    public interface IDomainModel : IDisposable
    {
        IQueryBuilder Query();
        void CompleteTransaction();
        void Update<T>(T dataObject) where T : IDataObject;
        void Create<T>(T dataObject) where T : IDataObject;
        void Delete<T>(long id) where T : IDataObject;
    }
}
