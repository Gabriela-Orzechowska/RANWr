using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using gablibela.io;

namespace gablibela
{
    namespace cmpr
    {
        public static class YAZ0
        {
            public static readonly string Signature = "Yaz0";
            public static readonly UInt32 SignatureHex = 0x59617A30;
            public static readonly byte DefaultCompressionLevel = 10;

            public static byte[] Decode(byte[] source, bool force = false)
            {
                MemoryStream stream = new MemoryStream(source);
                BetterBinaryReader reader = new BetterBinaryReader(stream);
                if (!force)
                {
                    string _signature = reader.ReadString(4, 0);
                    if (_signature != Signature) throw new Exception($"Invalid signature. Expected \"{Signature}\", found \"{_signature}\".");
                }
                UInt32 DecompressedSize = reader.ReadUInt32(0x4);

                List<byte> data = new List<byte>();
                reader.Seek(0x10);

                while (data.Count < DecompressedSize)
                {
                    byte Mask = reader.ReadByte();

                    for (Int32 i = 0; i < 8; i++)
                    {
                        if ((Mask & 0x80) == 0)
                        {
                            if (data.Count >= DecompressedSize) break;

                            byte ReadByte = reader.ReadByte();
                            Int32 Offset = (((byte)(ReadByte & 0x0F) << 8) | reader.ReadByte()) + 1;
                            Int32 Length = (ReadByte & 0xF0) == 0 ? reader.ReadByte() + 0x12 : (byte)((ReadByte & 0xF0) >> 4) + 2;

                            for (Int32 j = 0; j < Length; j++) data.Add(data[data.Count - Offset]);
                            
                        }
                        else data.Add(reader.ReadByte());
                        Mask = (byte)(Mask << 1);
                    }
                }
                return data.ToArray();
            }

            public static byte[] Compress(byte[] source, byte level = 10)
            {
                List<byte> data = new List<byte>();

                data.AddRange(Encoding.ASCII.GetBytes(Signature));
                data.AddRange(BitConverter.GetBytes(source.Length).Reverse());
                data.AddRange(new byte[8]);
                data.AddRange(Encode(source,level));

                return data.ToArray();
            }

            public static byte[] Encode(byte[] source, byte level = 10)
            {
                UInt32 sourceLenght = (UInt32)source.Length;

                UInt32 range = 0x1000;
                if(level < 10)
                    range = (UInt32)(0x10e0 * level / 9 - 0x0e0);
                if (level <= 0) 
                    range = 0;

                UInt32 position = 0;
                UInt32 sourceEnd = (UInt32)source.Length;

                byte[] destination = new byte[sourceEnd + (sourceEnd + 8) / 8];
                UInt32 destinationPosition = 0;
                UInt32 codeBytePosition = 0;

                UInt64 found = 0;
                UInt64 numBytes = 0;
                UInt32 diff;
                Int32 maxLenght = 0x111;

                while (position < sourceEnd)
                {
                    codeBytePosition = destinationPosition;
                    destination[destinationPosition] = 0; destinationPosition++;

                    for (Int32 i = 0; i < 8; i++)
                    {
                        if (position >= sourceEnd)
                            break;

                        numBytes = 1;
                        UInt64 search = 0;
                        if (range != 0)
                        {
                            search = Search(source, position, maxLenght, range, sourceEnd);
                            found = search >> 32;
                            numBytes = search & 0xFFFFFFFF;
                        }
                        if (numBytes < 3)
                        {
                            destination[codeBytePosition] |= (byte)(1 << (7 - i));
                            destination[destinationPosition] = source[position]; destinationPosition++; position++;
                        }
                        else
                        {
                            diff = (UInt32)(position - found - 1);

                            if (numBytes < 0x12)
                            {
                                destination[destinationPosition] = (byte)(diff >> 8 | (numBytes - 2) << 4); destinationPosition++;
                                destination[destinationPosition] = (byte)(diff & 0xFF); destinationPosition++;
                            }
                            else
                            {
                                destination[destinationPosition] = (byte)(diff >> 8); destinationPosition++;
                                destination[destinationPosition] = (byte)(diff & 0xFF); destinationPosition++;
                                destination[destinationPosition] = (byte)((numBytes - 0x12) & 0xFF); destinationPosition++;
                            }
                            position += (UInt32)numBytes;
                        }
                    }
                }

                byte[] result = new byte[destinationPosition];
                Array.Copy(destination, result, destinationPosition);
                return result;
            }

            public static UInt64 Search(byte[] source, UInt32 position, Int32 manLenght, UInt32 range, UInt32 sourceEnd)
            {
                UInt64 numBytes = 1;
                UInt64 found = 0;

                Int64 search;
                UInt32 compareEnd, compare1, compare2;
                byte c1;
                UInt32 lenght;

                if (position + 2 < sourceEnd)
                {
                    search = (position - range);
                    if (search < 0)
                        search = 0;

                    compareEnd = (UInt32)(position + manLenght);
                    if (compareEnd > sourceEnd)
                        compareEnd = sourceEnd;

                    c1 = source[position];
                    while (search < position)
                    {
                        search = Array.IndexOf(source, c1, (Int32)search, (Int32)(position - search));
                        if (search < 0)
                            break;

                        compare1 = (UInt32)(search + 1);
                        compare2 = position + 1;

                        while (compare2 < compareEnd && source[compare1] == source[compare2])
                        {
                            compare1++; compare2++;
                        }

                        lenght = compare2 - position;
                        if (numBytes < lenght)
                        {
                            numBytes = lenght;
                            found = (UInt32)search;
                            if ((Int64)numBytes == manLenght)
                                break;
                        }
                        search++;
                    }
                }
                return ((found << 32) | numBytes);
            }
        }
    }
}
