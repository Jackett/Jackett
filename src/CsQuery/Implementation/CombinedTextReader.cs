using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsQuery.Implementation
{
    /// <summary>
    /// Creates a virtual TextReader from several other streams.
    /// </summary>

    public class CombinedTextReader: TextReader
    {
        /// <summary>
        /// Create a new virtual TextReader by combining, in sequence, the streams provided as parameters to the constructor
        /// </summary>
        ///
        /// <param name="readers">
        /// A variable-length parameters list containing readers.
        /// </param>

        public CombinedTextReader(params TextReader[] readers)
        {
            Count=readers.Length;
            Readers  = readers;
            CurrentIndex = 0;
            
        }

        /// <summary>
        /// The readers.
        /// </summary>

        protected TextReader[] Readers;
        
        int CurrentIndex;
        int Count;

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.TextReader" /> and
        /// optionally releases the managed resources.
        /// </summary>
        ///
        /// <param name="disposing">
        /// true to release both managed and unmanaged resources; false to release only unmanaged
        /// resources.
        /// </param>

        protected override void Dispose(bool disposing)
        {
            foreach (var reader in Readers)
            {
                reader.Dispose();
            }
        }

        /// <summary>
        /// Reads the next character without changing the state of the reader or the character source.
        /// Returns the next available character without actually reading it from the input stream.
        /// </summary>
        ///
        /// <returns>
        /// An integer representing the next character to be read, or -1 if no more characters are
        /// available or the stream does not support seeking.
        /// </returns>

        public override int Peek()
        {
            int peek = Current.Peek();
            while (peek<0 && NextReader()) {
                peek = Current.Peek();
            }
            return peek;
        }


        
        /// <summary>
        /// Reads the next character from the input stream and advances the character position by one
        /// character.
        /// </summary>
        ///
        /// <returns>
        /// The next character from the input stream, or -1 if no more characters are available. The
        /// default implementation returns -1.
        /// </returns>

        public override int Read()
        {
            int val = Current.Read();
            while (val < 0 && NextReader())
            {
                return Current.Read();
            }
            return val;
        }

        /// <summary>
        /// Reads a maximum of <paramref name="count" /> characters from the current stream and writes
        /// the data to <paramref name="buffer" />, beginning at <paramref name="index" />.
        /// </summary>
        ///
        /// <param name="buffer">
        /// When this method returns, contains the specified character array with the values between
        /// <paramref name="index" /> and (<paramref name="index" /> + <paramref name="count" /> - 1)
        /// replaced by the characters read from the current source.
        /// </param>
        /// <param name="index">
        /// The position in <paramref name="buffer" /> at which to begin writing.
        /// </param>
        /// <param name="count">
        /// The maximum number of characters to read. If the end of the stream is reached before
        /// <paramref name="count" /> of characters is read into <paramref name="buffer" />, the current
        /// method returns.
        /// </param>
        ///
        /// <returns>
        /// The number of characters that have been read. The number will be less than or equal to
        /// <paramref name="count" />, depending on whether the data is available within the stream. This
        /// method returns zero if called when no more characters are left to read.
        /// </returns>
        ///
        /// ### <exception cref="T:System.ArgumentNullException">
        /// <paramref name="buffer" /> is null.
        /// </exception>
        /// ### <exception cref="T:System.ArgumentException">
        /// The buffer length minus <paramref name="index" /> is less than <paramref name="count" />.
        /// </exception>
        /// ### <exception cref="T:System.ArgumentOutOfRangeException">
        /// <paramref name="index" /> or <paramref name="count" /> is negative.
        /// </exception>
        /// ### <exception cref="T:System.ObjectDisposedException">
        /// The <see cref="T:System.IO.TextReader" /> is closed.
        /// </exception>
        /// ### <exception cref="T:System.IO.IOException">
        /// An I/O error occurs.
        /// </exception>

        public override int Read(char[] buffer, int index, int count)
        {
            int remaining = count;

            int val = Current.Read(buffer, index, count);
            remaining -= val;
            index += val;

            while (remaining > 0 && NextReader())
            {

                val = Current.Read(buffer, index, remaining);
                remaining -= val;
                index += val;
            }

            return count - remaining;
        }

        /// <summary>
        /// Reads a maximum of <paramref name="count" /> characters from the current stream, and writes
        /// the data to <paramref name="buffer" />, beginning at <paramref name="index" />.
        /// </summary>
        ///
        /// <param name="buffer">
        /// When this method returns, this parameter contains the specified character array with the
        /// values between <paramref name="index" /> and (<paramref name="index" /> +
        /// <paramref name="count" /> -1) replaced by the characters read from the current source.
        /// </param>
        /// <param name="index">
        /// The position in <paramref name="buffer" /> at which to begin writing.
        /// </param>
        /// <param name="count">
        /// The maximum number of characters to read.
        /// </param>
        ///
        /// <returns>
        /// The position of the underlying stream is advanced by the number of characters that were read
        /// into <paramref name="buffer" />.The number of characters that have been read. The number will
        /// be less than or equal to <paramref name="count" />, depending on whether all input characters
        /// have been read.
        /// </returns>


        public override int ReadBlock(char[] buffer, int index, int count)
        {
            int remaining = count;

            int val = Current.ReadBlock(buffer, index, count);
            remaining -= val;
            index += val;

            while (remaining > 0 && NextReader())
            {

                val = Current.ReadBlock(buffer, index, remaining);
                remaining -= val;
                index += val;
            }

            return count - remaining;
        }

     
        /// <summary>
        /// Reads a line of characters from the current stream and returns the data as a string. Note:
        /// this method will not combine data from two boundary streams into a single line; the end of a
        /// stream is always the end of a line. This could result in stream corruption (e.g. the addition
        /// of newlines between streams) when using this method.
        /// </summary>
        ///
        /// <returns>
        /// The next line from the input stream, or null if all characters have been read.
        /// </returns>

        public override string ReadLine()
        {
            var text = Current.ReadLine();

            while (text == null && NextReader())
            {
                text=Current.ReadLine();
            }
            return text;
        }

        /// <summary>
        /// Reads all characters from the current position to the end of the TextReader and returns them
        /// as one string.
        /// </summary>
        ///
        /// <returns>
        /// A string containing all characters from the current position to the end of the TextReader.
        /// </returns>

        public override string ReadToEnd()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append(Current.ReadToEnd() ?? "");
            while (NextReader())
            {
                sb.Append(Current.ReadToEnd() ?? "");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Advance to the next reader
        /// </summary>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        private bool NextReader()
        {
            if (CurrentIndex < Count - 1)
            {
                CurrentIndex++;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the current stream
        /// </summary>

        private TextReader Current
        {
            get
            {
                return Readers[CurrentIndex];
            }
        }

        /// <summary>
        /// Returns a hash code for this object.
        /// </summary>
        ///
        /// <returns>
        /// The hash code for this object.
        /// </returns>

        public override int GetHashCode()
        {
            int hash = 0; ;
            foreach (var reader in Readers)
            {
                hash += reader.GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Tests if this object is considered equal to another.
        /// </summary>
        ///
        /// <param name="obj">
        /// The object to compare to this object.
        /// </param>
        ///
        /// <returns>
        /// true if the objects are considered equal, false if they are not.
        /// </returns>

        public override bool Equals(object obj)
        {
            var other = obj as CombinedTextReader;
            if (other != null && other.Readers.Length == Readers.Length)
            {
                int cur = 0;
                foreach (var reader in other.Readers)
                {
                    if (!Readers[cur++].Equals(reader))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
    }
}

