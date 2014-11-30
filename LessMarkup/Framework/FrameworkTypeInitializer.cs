/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using Autofac;
using LessMarkup.Framework.Data;
using LessMarkup.Interfaces.Data;

namespace LessMarkup.Framework
{
    public class DataFrameworkTypeInitializer
    {
        public static void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DomainModelProvider>().As<IDomainModelProvider>().SingleInstance();
            builder.RegisterType<DomainModel>().As<IDomainModel>();
            builder.RegisterType<QueryBuilder>().As<IQueryBuilder>();
        }
    }
}
