using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace gablibela
{
    namespace snd
    {
            public class Util
            {
                [StructLayout(LayoutKind.Explicit)]
                public struct Table<T>
                {
                    [FieldOffset(0)] UInt32 Count;
                    [FieldOffset(4)] List<T> Items;

                    public Table(T type, UInt32 count)
                    {
                        Count = count;
                        Items = new List<T>();
                    }
                }

                public enum RefType
                {
                    REFTYPE_ADDRESS = 0,
                    REFTYPE_OFFSET = 1,
                };

                [StructLayout(LayoutKind.Sequential)]
                public class DataRef
                {
                    public byte refType; //RefType
                    public byte dataType;
                    public UInt16 reserved;
                    public UInt32 value;
                }

                public static UInt32 GetDataRefAddressImpl(RefType refType, UInt32 value, UInt32 baseAddress)
                {
                    if (refType == RefType.REFTYPE_ADDRESS) return value;
                    else return baseAddress + value;
                }
        }
    }
}
