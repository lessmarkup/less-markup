/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Framework.Data;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Framework.Helpers
{
    public static class DataHelper
    {
        public static int GetCollectionId(Type dataType)
        {
            return DomainModel.GetCollectionId(dataType);
        }

        public static int GetCollectionId<T>() where T : IDataObject
        {
            return DomainModel.GetCollectionId<T>();
        }

        public static Type GetCollectionType(int collectionId)
        {
            return DomainModel.GetCollectionType(collectionId);
        }
    }
}
