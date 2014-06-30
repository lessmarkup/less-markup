/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.DataFramework.DataObjects
{
    [AttributeUsage(AttributeTargets.Class)]
    public class EntityAttribute : Attribute
    {
        private readonly EntityType _entityType;

        public EntityAttribute(EntityType entityType)
        {
            _entityType = entityType;
        }

        public EntityType EntityType { get { return _entityType; } }
    }
}
