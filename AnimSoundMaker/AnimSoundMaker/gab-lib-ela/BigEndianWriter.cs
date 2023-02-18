using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Printing.IndexedProperties;
using System.Text;
using static System.BitConverter;
using static System.Text.Encoding;

namespace AnimSoundMaker
{
    public class BigEndianWriter
    {
        private static Stream _stream;
        private static Encoding _encoding;

        public BigEndianWriter(Stream input) : this(input, Default)
        {
        }
        public BigEndianWriter(Stream input, Encoding encoding)
        {
            _stream = input;
            _encoding = encoding;
        }

        public void Write(byte[] buffer, ulong? offset = null, int index = 0, int? count = null)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if(count == null) 
                count = buffer.Length;

            long start = _stream.Position;
            if (offset != null) _stream.Position += (long)offset;

            Contract.EndContractBlock();
            _stream.Write(buffer, index, (int)count);
            if (offset != null) _stream.Position = start;
            
        }

        public void Write(byte value, ulong? offset = null)
        {
            byte[] _buffer = new byte[1] { value };
            Write(_buffer, offset);
        }

        public void Write(Int16 value, ulong? offset = null)
        {
            byte[] _buffer = Flip(GetBytes(value));
            Write(_buffer, offset);
        }

        public void Write(UInt16 value, ulong? offset = null)
        {
            byte[] _buffer = Flip(GetBytes(value));
            Write(_buffer, offset);
        }

        public void Write(Int32 value, ulong? offset = null)
        {
            byte[] _buffer = Flip(GetBytes(value));
            Write(_buffer, offset);
        }

        public void Write(UInt32 value, ulong? offset = null)
        {
            byte[] _buffer = Flip(GetBytes(value));
            Write(_buffer, offset);
        }

        public void Write(Int64 value, ulong? offset = null)
        {
            byte[] _buffer = Flip(GetBytes(value));
            Write(_buffer, offset);
        }

        public void Write(UInt64 value, ulong? offset = null)
        {
            byte[] _buffer = Flip(GetBytes(value));
            Write(_buffer, offset);
        }

        public void Write(float value, ulong? offset = null)
        {
            byte[] _buffer = Flip(GetBytes(value));
            Write(_buffer, offset);
        }

        public void Write(String value, ulong? offset = null)
        {
            byte[] _buffer = _encoding.GetBytes(value);
            Write(_buffer, offset);
        }



        public void CloseStream() => _stream.Close();

        private static byte[] Flip(byte[] value)
        {
            Array.Reverse(value);
            return value;
        }


        public long Length() => _stream.Length;

        public long Position() => _stream.Position;
    }
}
