/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.DataFramework.DataAccess;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Framework.Helpers
{
    public static class DataHelper
    {
        public static int? GetCollectionId(Type dataType)
        {
            return AbstractDomainModel.GetCollectionId(dataType);
        }

        public static int? GetCollectionId<T>() where T : IDataObject
        {
            return AbstractDomainModel.GetCollectionId<T>();
        }

        public static int GetCollectionIdVerified(Type dataType)
        {
            return AbstractDomainModel.GetCollectionIdVerified(dataType);
        }

        public static int GetCollectionIdVerified<T>() where T : IDataObject
        {
            return AbstractDomainModel.GetCollectionIdVerified<T>();
        }

        public static Type GetCollectionType(int collectionId)
        {
            return AbstractDomainModel.GetCollectionType(collectionId);
        }
    }
}
