/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityAttribute : Attribute
    {
        private readonly Type _collectionType;

        public EntityAttribute(Type collectionType)
        {
            _collectionType = collectionType;
        }

        public Type CollectionType { get { return _collectionType; } }
    }
}
