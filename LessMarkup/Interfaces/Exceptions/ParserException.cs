/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Runtime.Serialization;

namespace LessMarkup.Interfaces.Exceptions
{
    public class ParserException : Exception
    {
        public ParserException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        public ParserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public ParserException(string message)
            : base(message)
        {
        }

        public ParserException()
        { }
    }
}
