/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace LessMarkup.Interfaces.Composite
{
    public abstract class AbstractElement
    {
        public string Type
        {
            get
            {
                var type = GetType().Name;
                if (type.EndsWith("Element"))
                {
                    type = type.Substring(0, type.Length - "Element".Length);
                }
                return type;
            }
        }
    }
}
