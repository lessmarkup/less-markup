/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System.IO;

namespace LessMarkup.Engine.Response
{
    public class ResponseFilterStream : Stream
    {
        private readonly Stream _baseFilter;
        private long _bytesWritten;

        public long BytesWritten { get { return _bytesWritten; } }

        public ResponseFilterStream(Stream baseFilter)
        {
            _baseFilter = baseFilter;
        }

        public override void Flush()
        {
            _baseFilter.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _baseFilter.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _baseFilter.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _baseFilter.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _baseFilter.Write(buffer, offset, count);
            _bytesWritten += count;
        }

        public override bool CanRead
        {
            get { return _baseFilter.CanRead; }
        }

        public override bool CanSeek
        {
            get { return _baseFilter.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return _baseFilter.CanWrite; }
        }

        public override long Length
        {
            get { return _baseFilter.Length; }
        }

        public override long Position
        {
            get { return _baseFilter.Position; } 
            set { _baseFilter.Position = value; }
        }
    }
}
