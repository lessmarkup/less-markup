/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

namespace LessMarkup.Interfaces.RecordModel
{
    public class InputFile
    {
        public string Type { get; set; }
        public byte[] File { get; set; }
        public string Name { get; set; }
    }
}
