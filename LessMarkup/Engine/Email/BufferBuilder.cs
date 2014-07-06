/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.IO;

namespace LessMarkup.Engine.Email
{
    public class BufferBuilder
    {
        private int _offset;
        private byte[] _data;

        public BufferBuilder()
            : this(32768)
        {
        }

        public BufferBuilder(int initialSize)
        {
            _data = new byte[initialSize];
        }

        public int Length
        {
            get { return _offset; }
        }

        public byte[] Data { get { return _data; } }

        public void Reset()
        {
            _offset = 0;
        }

        public void Remove(int size)
        {
            if (size >= _offset)
            {
                _offset = 0;
                return;
            }

            Buffer.BlockCopy(_data, size, _data, 0, _offset - size);
            _offset -= size;
        }

        public void Append(Stream stream, int size)
        {
            MakeAvailable(size);
            size = stream.Read(_data, _offset, size);
            _offset += size;
        }

        public void Append(byte[] data)
        {
            Append(data, 0, data.Length);
        }

        public void Append(byte[] data, int offset, int size)
        {
            if (size <= 0)
            {
                return;
            }

            MakeAvailable(size);

            Buffer.BlockCopy(data, offset, _data, _offset, size);
            _offset += size;
        }

        private void MakeAvailable(int bytes)
        {
            int newLength = _data.Length;

            while (newLength - _offset < bytes)
            {
                newLength *= 2;
            }

            if (newLength == _data.Length)
            {
                return;
            }

            var newData = new byte[newLength];

            if (_offset > 0)
            {
                Buffer.BlockCopy(_data, 0, newData, 0, _offset);
            }

            _data = newData;
        }
    }
}
