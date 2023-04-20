using System;
using System.Runtime.InteropServices;

namespace gablibela 
{ 
    namespace ut
    {
        public class binaryFileFormats
        {
            [StructLayout(LayoutKind.Explicit)]
            public struct BinaryFileHeader
            {
                [FieldOffset(0x0)] public UInt32 Signature;
                [FieldOffset(0x4)] public UInt16 ByteOrder;
                [FieldOffset(0x6)] public UInt16 Version;
                [FieldOffset(0x8)] public UInt32 FileSize;
                [FieldOffset(0xC)] public UInt16 HeaderSize;
                [FieldOffset(0x10)] public UInt16 DataBlocks;
            }

            [StructLayout(LayoutKind.Explicit)]
            public struct BinaryBlockHeader
            {
                [FieldOffset(0x0)] public UInt32 Kind;
                [FieldOffset(0x4)] public UInt16 Size;
            }
            public static bool IsValidBinaryFile(BinaryFileHeader header, UInt32 signature, UInt16 version, UInt16 minBlock = 1)
            {
                return (header.Signature == signature) && (header.Version <= version) && (header.DataBlocks >= minBlock);
            }
        }
        
    }
}
