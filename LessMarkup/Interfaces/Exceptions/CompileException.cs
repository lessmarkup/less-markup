/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.Collections.Generic;

namespace LessMarkup.Interfaces.Exceptions
{
    public class CompileException : ExtendedMessageException
    {
        private readonly List<string> _errors; 

        public CompileException(List<string> errors) : base("Cannot Compile", GenerateMessage(errors))
        {
            _errors = errors;
        }

        public List<string> Errors { get { return _errors; } } 

        private static string GenerateMessage(IEnumerable<string> errors)
        {
            var ret = "Compilation Errors:\r\n";

            foreach (var error in errors)
            {
                ret += "\r\n" + error;
            }

            return ret;
        }
    }
}
