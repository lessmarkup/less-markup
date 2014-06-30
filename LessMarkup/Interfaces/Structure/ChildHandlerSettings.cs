/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace LessMarkup.Interfaces.Structure
{
    public class ChildHandlerSettings
    {
        public IPageHandler Handler { get; set; }
        public long? Id { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        public string Rest { get; set; }
    }
}
