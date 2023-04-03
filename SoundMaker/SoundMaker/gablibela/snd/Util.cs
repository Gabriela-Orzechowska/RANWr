using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gablibela
{
    namespace snd
    {
        public class Util
        {
            public static UInt16 VOLUME_MIN = 0x0000; // -90.4dB
            public static UInt16 VOLUME_MAX = 0xff64;   // +6.0dB
            public static int VOLUME_DB_MIN = -904;     // -90.4dB = -inf
            public static int VOLUME_DB_MAX = 60;       // +6.0dB

            public static int PAN_MIN = 0;
            public static int PAN_MAX = 127;
            public static int PAN_CENTER = 64;

            public static int PITCH_DIVISION_BIT = 8;                        // Semitone resolution (bit count)
            public static int PITCH_DIVISION_RANGE = 1 << PITCH_DIVISION_BIT;  // Semitone resolution
            public static int CALC_DECIBEL_SCALE_MAX = 127;

            public static int PAN_CURVE_NUM = 3;
            public enum PanCurve
            {
                PAN_CURVE_SQRT,
                PAN_CURVE_SINCOS,
                PAN_CURVE_LINEAR
            };
            public struct PanInfo
            {
                PanCurve curve;
                bool centerZeroFlag;    // Whether to set the center to 0 db.
                bool zeroClampFlag;     // Whether to clamp when 0 db is exceeded.
            };


            private static int OCTAVE_DIVISION = 12;

            private static int VOLUME_TABLE_SIZE = VOLUME_DB_MAX - VOLUME_DB_MIN + 1;
            private static int DECIBEL_TABLE_SIZE = 128;
            private static int DECIBEL_SQUARE_TABLE_SIZE = 128;

            private static int PAN_TABLE_MAX = 256;
            private static int PAN_TABLE_MIN = 0;
            private static int PAN_TABLE_CENTER = 128;
            private static int PAN_TABLE_SIZE = PAN_TABLE_MAX - PAN_TABLE_MIN + 1;

            private static int COEF_TABLE_MIN = 0;
            private static int COEF_TABLE_MAX = 127;
            private static int COEF_TABLE_SIZE = COEF_TABLE_MAX - COEF_TABLE_MIN + 1;
            private static int IIR_COEF_COUNT = 2;
            private static int BIQUAD_COEF_COUNT = 5;

            // pitch table
            private static float[] NoteTable = new float[OCTAVE_DIVISION];
            private static float[] PitchTable = new float[PITCH_DIVISION_RANGE];

            // dB -> Ratio Table
            private static float[] Decibel2RatioTable = new float[VOLUME_TABLE_SIZE];

            private static float[] Pan2RatioTableSqrt = new float[PAN_TABLE_SIZE];
            private static float[] Pan2RatioTableSinCos = new float[PAN_TABLE_SIZE];
            private static float[] Pan2RatioTableLinear = new float[PAN_TABLE_SIZE];

            // Remote Filter Coef
            private static UInt16[,] RemoteFilterCoefTable = new UInt16[COEF_TABLE_SIZE,BIQUAD_COEF_COUNT];

            public class Table<T>
            {
                UInt32 Count;
                List<T> Items;

                public Table(T type, UInt32 count)
                {
                    Count = count;
                    Items = new List<T>();
                }
            }

            public enum RefType
            {
                REFTYPE_ADDRESS = 0,
                REFTYPE_OFFSET = 1
            };

            public class DataRef
            {
                public byte refType; //RefType
                public byte dataType;
                public UInt16 reserved;
                public UInt32 value;

                private Type[] types;

                public DataRef(byte refType, byte dataType, ushort reserved, uint value, Type[] types)
                {
                    this.refType = refType;
                    this.dataType = dataType;
                    this.reserved = reserved;
                    this.value = value;
                    this.types = types;
                }
                public DataRef(Type[] types, uint value)
                {
                    this.types = types;
                    this.refType = 1;
                    this.reserved = 0;
                    this.value = value;
                    this.dataType = 0;
                }
            }
        }
    }
}
