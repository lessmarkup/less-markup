/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Runtime.Serialization;

namespace LessMarkup.Interfaces.Exceptions
{
    public class TextNotFoundException : Exception
    {
        public TextNotFoundException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        public TextNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public TextNotFoundException(string message)
            : base(message)
        {
        }

        public TextNotFoundException()
        { }
    }
}
