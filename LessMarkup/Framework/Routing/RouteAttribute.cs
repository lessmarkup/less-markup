/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Framework.Routing
{
    public class RouteAttribute : Attribute
    {
        public string Constraints { get; set; }

        public Type[] ConstraintTypes { get; set; }

        public string Defaults { get; set; }

        public string Pattern { get; set; }

        public string Name { get; set; }

        public RouteAttribute(string name, string pattern)
        {
            Name = name;
            Pattern = pattern;
        }
    }
}
