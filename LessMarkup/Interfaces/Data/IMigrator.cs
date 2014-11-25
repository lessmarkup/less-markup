using System;

namespace LessMarkup.Interfaces.Data
{
    public interface IMigrator
    {
        void ExecuteSql(string sql);
        void CreateTable<T>() where T : IDataObject;
        void AddDependency<TD, TB>(string column = null) where TD : IDataObject where TB: IDataObject;
        void DeleteDependency<TD, TB>(string column = null) where TD : IDataObject where TB : IDataObject;
        void UpdateTable<T>() where T : IDataObject;
        void DeleteTable<T>() where T : IDataObject;
    }
}
