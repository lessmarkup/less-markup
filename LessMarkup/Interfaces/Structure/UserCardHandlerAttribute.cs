/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;

namespace LessMarkup.Interfaces.Structure
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UserCardHandlerAttribute : Attribute
    {
        public object TitleTextId { get; set; }
        public string Path { get; set; }

        public UserCardHandlerAttribute(object titleTextId)
        {
            TitleTextId = titleTextId;
        }
    }
}
