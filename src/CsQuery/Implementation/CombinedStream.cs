#region Copyright 2010-2012 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
/*
 * Changes made by J. Treworgy from source forked on 12/7/2012
 */
#endregion
using System;
using System.Collections.Generic;
using System.IO;

namespace CsQuery.Implementation
{
    /// <summary>
    /// Creates a single virtual stream out of multiple input streams.
    /// </summary>

    public class CombinedStream : BaseStream
    {
        bool _valid;
        readonly IEnumerator<Stream> _streams;

        /// <summary>
        /// Creates a single virtual stream out of multiple input streams.
        /// </summary>
        ///
        /// <param name="streams">
        /// The streams.
        /// </param>

        public CombinedStream(params Stream[] streams) : 
            this((IEnumerable<Stream>)streams) 
        { }

        /// <summary>
        /// Creates a single virtual stream out of multiple input streams.
        /// </summary>
        ///
        /// <param name="streams">
        /// The streams.
        /// </param>

        public CombinedStream(IEnumerable<Stream> streams)
        {
            _streams = streams.GetEnumerator();
            _valid = _streams.MoveNext();
        }

        /// <summary>
        /// Gets a value indicating whether we can read.
        /// </summary>

        public override bool CanRead { 
            get {
                return _valid && _streams.Current.CanRead; 
            } 
        }
        /// <summary> Reads from the next stream available </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0) return 0;
            while (_valid)
            {
                int len = _streams.Current.Read(buffer, offset, count);
                if (len > 0)
                    return len;

                //_streams.Current.Dispose();
                _valid = _streams.MoveNext();
            }

            return 0;
        }
        /// <summary> Disposes of all remaining streams. </summary>
        //protected override void Dispose(bool disposing)
        //{
        //    while (disposing && _valid)
        //    {
        //        _streams.Current.Dispose();
        //        _valid = _streams.MoveNext();
        //    }

        //    base.Dispose(disposing);
        //}
    }

    /// <summary>
    /// Base stream implementation
    /// </summary>

    public abstract class BaseStream : Stream
    {
        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead { get { return false; } }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek { get { return false; } }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite { get { return false; } }

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        public override long Length { get { throw new NotSupportedException(); } }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        { }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Reads a byte from the stream and advances the position within the stream by one byte, or returns -1 if at the end of the stream.
        /// </summary>
        public override int ReadByte()
        {
            byte[] bytes = new byte[1];
            return Read(bytes, 0, 1) == 1 ? bytes[0] : -1;
        }

        /// <summary>
        /// Writes a byte to the current position in the stream and advances the position within the stream by one byte.
        /// </summary>
        public override void WriteByte(byte value)
        {
            Write(new byte[] { value }, 0, 1);
        }
    }
}