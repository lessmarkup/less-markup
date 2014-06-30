/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using Autofac;

namespace LessMarkup.DataFramework
{
    public static class DependencyResolver
    {
        public static void SetResolver(ILifetimeScope resolver)
        {
            LifetimeScope = resolver;
        }

        private static ILifetimeScope LifetimeScope { get; set; }

        public static T Resolve<T>()
        {
            return LifetimeScope.Resolve<T>();
        }

        public static object Resolve(Type type)
        {
            return LifetimeScope.Resolve(type);
        }
    }
}
