using System;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Framework.Helpers
{
    public static class DataHelper
    {
        public static int GetCollectionId(Type dataType)
        {
            return AbstractDomainModel.GetCollectionId(dataType);
        }

        public static int GetCollectionId<T>() where T : IDataObject
        {
            return AbstractDomainModel.GetCollectionId<T>();
        }

        public static Type GetCollectionType(int collectionId)
        {
            return AbstractDomainModel.GetCollectionType(collectionId);
        }
    }
}
