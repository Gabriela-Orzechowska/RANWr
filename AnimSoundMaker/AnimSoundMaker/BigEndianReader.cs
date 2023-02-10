using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Documents;
using static System.BitConverter;
using static System.Text.Encoding;

namespace AnimSoundMaker
{
    public class BigEndianReader
    {
        private static Stream _stream;
        private static Encoding _encoding;

        public BigEndianReader(Stream input): this(input, Default) {
        }
        public BigEndianReader(Stream input, Encoding encoding)
        {
            _stream = input;
            _encoding = encoding;
        }

        public byte[] ReadBytes(int count, long? offset = null)
        {
            if (_stream == null) throw new FileNotFoundException();
            if (count == 0) return Array.Empty<byte>();
            long start = _stream.Position;
            if (offset != null) _stream.Position += (long) offset;
            byte[] result = new byte[count];

            int numRead = 0;
            do {
                int n = _stream.Read(result, numRead, count);
                if (n == 0)
                    break;
                numRead += n;
                count -= n;
            } while (count > 0);
            
            if (numRead != result.Length) {
                byte[] copy = new byte[numRead];
                Buffer.BlockCopy(result, 0, copy, 0, numRead);
                result = copy;
            }
            if (offset != null) _stream.Position = start;
            return result;

        }
        public string ReadString(ushort lenght, long? offset = null) => _encoding.GetString(ReadBytes(lenght,offset));

        public string ReadStringNT(long? offset = null)
        {
            if (_stream == null) throw new FileNotFoundException();
            long start = _stream.Position;
            if (offset != null) _stream.Position += (long)offset;
            List<byte> strBytes = new List<byte>();
            int b;
            while((b = _stream.ReadByte()) != 0)
            {
                strBytes.Add((byte)b);
            }
            string output = _encoding.GetString(strBytes.ToArray());
            if(output.EndsWith('\0')) output.Substring(0, output.Length - 1);
            if (offset != null) _stream.Position = start;
            return output;
        }

        public ushort ReadUInt8(long? offset = null) => ToUInt16(Flip(ReadBytes(1,offset)), 0);

        public ushort ReadUInt16(long? offset = null) => ToUInt16(Flip(ReadBytes(2,offset)), 0);

        public uint ReadUInt32(long? offset = null) => ToUInt32(Flip(ReadBytes(4,offset)), 0);

        public short ReadInt8(long? offset = null) => ReadBytes(1,offset)[0];

        public short ReadInt16(long? offset = null) => ToInt16(Flip(ReadBytes(2,offset)), 0);

        public int ReadInt32(long? offset = null) => ToInt32(Flip(ReadBytes(4,offset)), 0);

        public float ReadFloat(long? offset = null) => ToSingle(Flip(ReadBytes(4,offset)), 0);

        public T ReadEnum<T>(long? offset = null) where T : Enum => (T)(object)(int)ReadByte(offset);

        public byte ReadByte(long? offset = null) => ReadBytes(1,offset)[0];

        private static byte[] Flip(byte[] value)
        {
            Array.Reverse(value);
            return value;
        }

        public long Length() => _stream.Length;

        public long Position() => _stream.Position;
    }
}
