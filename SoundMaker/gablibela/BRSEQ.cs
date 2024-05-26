
using System.Runtime.InteropServices;

namespace gablibela
{
    namespace snd
    {
        class BRSEQ
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Header
            {
                public UInt32 Magic;
                public UInt16 BOM;
                public UInt16 Version;
                public UInt32 Size;
                public UInt16 HeaderSize;
                public UInt16 SectionCount;
                public UInt32 DataOffset;
                public UInt32 DataSize;
                public UInt32 LablOffset;
                public UInt32 LablSize;
            }
            [StructLayout(LayoutKind.Sequential)]
            public struct DataHeader
            {
                public UInt32 Magic;
                public UInt32 Size;
                public UInt32 Offset;
            }
            [StructLayout(LayoutKind.Sequential)]
            public struct LablHeader 
            { 
                
            }
        }
    }

}
