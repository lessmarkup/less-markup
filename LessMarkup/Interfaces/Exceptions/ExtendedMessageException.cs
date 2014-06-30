/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Runtime.Serialization;

namespace LessMarkup.Interfaces.Exceptions
{
    public class ExtendedMessageException : Exception
    {
        private readonly string _extendedMessage;

        public ExtendedMessageException(SerializationInfo serializationInfo, StreamingContext streamingContext)
            : base(serializationInfo, streamingContext)
        {
        }

        public ExtendedMessageException(string message, string extendedMessage, Exception innerException)
            : base(message, innerException)
        {
            _extendedMessage = extendedMessage;
        }

        public ExtendedMessageException(string message, string extendedMessage)
            : base(message)
        {
            _extendedMessage = extendedMessage;
        }

        public string ExtendedMessage {  get { return _extendedMessage; }}
    }
}
