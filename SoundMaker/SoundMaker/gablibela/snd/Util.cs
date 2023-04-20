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
        namespace detail {

            public class Util
            {
                public const UInt16 VOLUME_MIN = 0x0000; // -90.4dB
                public const UInt16 VOLUME_MAX = 0xff64;   // +6.0dB
                public const int VOLUME_DB_MIN = -904;     // -90.4dB = -inf
                public const int VOLUME_DB_MAX = 60;       // +6.0dB

                public const int PAN_MIN = 0;
                public const int PAN_MAX = 127;
                public const int PAN_CENTER = 64;

                public const int PITCH_DIVISION_BIT = 8;                        // Semitone resolution (bit count)
                public const int PITCH_DIVISION_RANGE = 1 << PITCH_DIVISION_BIT;  // Semitone resolution
                public const int CALC_DECIBEL_SCALE_MAX = 127;

                public const int PAN_CURVE_NUM = 3;
                public enum PanCurve
                {
                    PAN_CURVE_SQRT,
                    PAN_CURVE_SINCOS,
                    PAN_CURVE_LINEAR
                };
                public struct PanInfo
                {
                    public PanCurve curve;
                    public bool centerZeroFlag;    // Whether to set the center to 0 db.
                    public bool zeroClampFlag;     // Whether to clamp when 0 db is exceeded.
                };

                private const int OCTAVE_DIVISION = 12;

                private const int VOLUME_TABLE_SIZE = VOLUME_DB_MAX - VOLUME_DB_MIN + 1;
                private const int DECIBEL_TABLE_SIZE = 128;
                private const int DECIBEL_SQUARE_TABLE_SIZE = 128;

                private const int PAN_TABLE_MAX = 256;
                private const int PAN_TABLE_MIN = 0;
                private const int PAN_TABLE_CENTER = 128;
                private const int PAN_TABLE_SIZE = PAN_TABLE_MAX - PAN_TABLE_MIN + 1;

                private const int COEF_TABLE_MIN = 0;
                private const int COEF_TABLE_MAX = 127;
                private const int COEF_TABLE_SIZE = COEF_TABLE_MAX - COEF_TABLE_MIN + 1;
                private const int IIR_COEF_COUNT = 2;
                private const int BIQUAD_COEF_COUNT = 5;

                // pitch table
                private static readonly float[] NoteTable = {
                    1.00000000000000f,
                    1.05946309435930f,
                    1.12246204830937f,
                    1.18920711500272f,
                    1.25992104989487f,
                    1.33483985417003f,
                    1.41421356237310f,
                    1.49830707687668f,
                    1.58740105196820f,
                    1.68179283050743f,
                    1.78179743628068f,
                    1.88774862536339f
                };
                private static readonly float[] PitchTable = new float[PITCH_DIVISION_RANGE] {
                    1.00000000000000f, 1.00022565930507f, 1.00045136953226f, 1.00067713069307f,
                    1.00090294279898f, 1.00112880586149f, 1.00135471989211f, 1.00158068490233f,
                    1.00180670090365f, 1.00203276790759f, 1.00225888592566f, 1.00248505496936f,
                    1.00271127505020f, 1.00293754617972f, 1.00316386836942f, 1.00339024163082f,
                    1.00361666597546f, 1.00384314141486f, 1.00406966796055f, 1.00429624562407f,
                    1.00452287441694f, 1.00474955435071f, 1.00497628543691f, 1.00520306768709f,
                    1.00542990111280f, 1.00565678572558f, 1.00588372153699f, 1.00611070855857f,
                    1.00633774680189f, 1.00656483627850f, 1.00679197699996f, 1.00701916897784f,
                    1.00724641222370f, 1.00747370674912f, 1.00770105256566f, 1.00792844968490f,
                    1.00815589811842f, 1.00838339787779f, 1.00861094897460f, 1.00883855142043f,
                    1.00906620522687f, 1.00929391040551f, 1.00952166696794f, 1.00974947492577f,
                    1.00997733429057f, 1.01020524507396f, 1.01043320728755f, 1.01066122094292f,
                    1.01088928605170f, 1.01111740262549f, 1.01134557067591f, 1.01157379021458f,
                    1.01180206125310f, 1.01203038380312f, 1.01225875787623f, 1.01248718348409f,
                    1.01271566063830f, 1.01294418935052f, 1.01317276963236f, 1.01340140149547f,
                    1.01363008495149f, 1.01385882001206f, 1.01408760668882f, 1.01431644499343f,
                    1.01454533493752f, 1.01477427653277f, 1.01500326979081f, 1.01523231472332f,
                    1.01546141134194f, 1.01569055965835f, 1.01591975968421f, 1.01614901143119f,
                    1.01637831491095f, 1.01660767013518f, 1.01683707711556f, 1.01706653586375f,
                    1.01729604639144f, 1.01752560871032f, 1.01775522283207f, 1.01798488876839f,
                    1.01821460653096f, 1.01844437613148f, 1.01867419758165f, 1.01890407089317f,
                    1.01913399607774f, 1.01936397314707f, 1.01959400211286f, 1.01982408298683f,
                    1.02005421578069f, 1.02028440050616f, 1.02051463717495f, 1.02074492579879f,
                    1.02097526638940f, 1.02120565895850f, 1.02143610351784f, 1.02166660007913f,
                    1.02189714865412f, 1.02212774925453f, 1.02235840189212f, 1.02258910657863f,
                    1.02281986332579f, 1.02305067214536f, 1.02328153304909f, 1.02351244604873f,
                    1.02374341115603f, 1.02397442838276f, 1.02420549774068f, 1.02443661924155f,
                    1.02466779289714f, 1.02489901871921f, 1.02513029671954f, 1.02536162690990f,
                    1.02559300930208f, 1.02582444390784f, 1.02605593073898f, 1.02628746980727f,
                    1.02651906112451f, 1.02675070470248f, 1.02698240055299f, 1.02721414868781f,
                    1.02744594911876f, 1.02767780185764f, 1.02790970691624f, 1.02814166430638f,
                    1.02837367403986f, 1.02860573612850f, 1.02883785058410f, 1.02907001741849f,
                    1.02930223664349f, 1.02953450827092f, 1.02976683231260f, 1.02999920878037f,
                    1.03023163768604f, 1.03046411904146f, 1.03069665285846f, 1.03092923914889f,
                    1.03116187792457f, 1.03139456919736f, 1.03162731297909f, 1.03186010928163f,
                    1.03209295811682f, 1.03232585949652f, 1.03255881343258f, 1.03279181993686f,
                    1.03302487902123f, 1.03325799069755f, 1.03349115497769f, 1.03372437187351f,
                    1.03395764139691f, 1.03419096355973f, 1.03442433837388f, 1.03465776585123f,
                    1.03489124600365f, 1.03512477884305f, 1.03535836438130f, 1.03559200263031f,
                    1.03582569360196f, 1.03605943730815f, 1.03629323376078f, 1.03652708297176f,
                    1.03676098495299f, 1.03699493971638f, 1.03722894727384f, 1.03746300763728f,
                    1.03769712081862f, 1.03793128682977f, 1.03816550568267f, 1.03839977738923f,
                    1.03863410196138f, 1.03886847941105f, 1.03910290975017f, 1.03933739299068f,
                    1.03957192914452f, 1.03980651822362f, 1.04004116023993f, 1.04027585520539f,
                    1.04051060313196f, 1.04074540403158f, 1.04098025791621f, 1.04121516479780f,
                    1.04145012468832f, 1.04168513759972f, 1.04192020354397f, 1.04215532253304f,
                    1.04239049457890f, 1.04262571969352f, 1.04286099788887f, 1.04309632917694f,
                    1.04333171356970f, 1.04356715107914f, 1.04380264171725f, 1.04403818549601f,
                    1.04427378242741f, 1.04450943252346f, 1.04474513579614f, 1.04498089225746f,
                    1.04521670191942f, 1.04545256479402f, 1.04568848089328f, 1.04592445022919f,
                    1.04616047281379f, 1.04639654865907f, 1.04663267777707f, 1.04686886017980f,
                    1.04710509587929f, 1.04734138488756f, 1.04757772721665f, 1.04781412287858f,
                    1.04805057188539f, 1.04828707424912f, 1.04852362998181f, 1.04876023909550f,
                    1.04899690160224f, 1.04923361751407f, 1.04947038684306f, 1.04970720960124f,
                    1.04994408580069f, 1.05018101545345f, 1.05041799857160f, 1.05065503516719f,
                    1.05089212525229f, 1.05112926883898f, 1.05136646593932f, 1.05160371656540f,
                    1.05184102072929f, 1.05207837844307f, 1.05231578971883f, 1.05255325456865f,
                    1.05279077300463f, 1.05302834503885f, 1.05326597068341f, 1.05350364995040f,
                    1.05374138285194f, 1.05397916940012f, 1.05421700960704f, 1.05445490348482f,
                    1.05469285104557f, 1.05493085230140f, 1.05516890726443f, 1.05540701594677f,
                    1.05564517836056f, 1.05588339451791f, 1.05612166443095f, 1.05635998811181f,
                    1.05659836557263f, 1.05683679682555f, 1.05707528188269f, 1.05731382075621f,
                    1.05755241345824f, 1.05779106000093f, 1.05802976039644f, 1.05826851465692f,
                    1.05850732279451f, 1.05874618482139f, 1.05898510074970f, 1.05922407059161f
                };

                private static readonly float[][] PanTableTable =
                {
                    Pan2RatioTableSqrt,
                    Pan2RatioTableSinCos,
                    Pan2RatioTableLinear
                };

                // dB -> Ratio Table
                private static float[] Decibel2RatioTable = new float[VOLUME_TABLE_SIZE];

                private static float[] Pan2RatioTableSqrt = new float[PAN_TABLE_SIZE];
                private static float[] Pan2RatioTableSinCos = new float[PAN_TABLE_SIZE];
                private static float[] Pan2RatioTableLinear = new float[PAN_TABLE_SIZE];

                // Remote Filter Coef
                private static UInt16[,] RemoteFilterCoefTable = new UInt16[COEF_TABLE_SIZE, BIQUAD_COEF_COUNT];

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

                [StructLayout(LayoutKind.Explicit, Size=0x8)]
                public class DataRef
                {
                    [FieldOffset(0x0)] public byte refType; //RefType
                    [FieldOffset(0x1)] public byte dataType;
                    [FieldOffset(0x2)] public UInt16 reserved;
                    [FieldOffset(0x4)] public UInt32 value;
                }

                public static float CalcPitchRatio(int pitch)
                {
                    float ratio;

                    int octave = 0;
                    float octave_float = 1.0f;
                    int note;

                    while (pitch < 0)
                    {
                        octave--;
                        pitch += PITCH_DIVISION_RANGE * OCTAVE_DIVISION;
                    }
                    while (pitch >= PITCH_DIVISION_RANGE * OCTAVE_DIVISION)
                    {
                        octave++;
                        pitch -= PITCH_DIVISION_RANGE * OCTAVE_DIVISION;
                    }
                    while (octave > 0)
                    {
                        octave_float *= 2.0f;
                        octave--;
                    }
                    while (octave < 0)
                    {
                        octave_float /= 2.0f;
                        octave++;
                    }

                    note = pitch / PITCH_DIVISION_RANGE;
                    pitch %= PITCH_DIVISION_RANGE;

                    ratio = octave_float;
                    if (note != 0) ratio *= NoteTable[note];
                    if (pitch != 0) ratio *= PitchTable[pitch];

                    return ratio;
                }

                public static float CalcVolumeRatio(float dB)
                {
                    dB = Math.Clamp(dB, -90.4f, 6.0f);
                    return Decibel2RatioTable[(int)(dB * 10) - VOLUME_DB_MIN];
                }
                public static float CalcPanRatio(float pan, PanInfo info )
                {
                    pan = (Math.Clamp(pan, -1.0f, 1.0f ) + 1.0f ) / 2.0f; // Clamp to 0.0 - 1.0 scale

                    float[] table = PanTableTable[(int)info.curve];

                    float ratio = table[(int)(pan * PAN_TABLE_MAX + 0.5f)];
                    if (info.centerZeroFlag ) ratio /= table[PAN_TABLE_CENTER]; // Make the center come out to 1.0 (100%)

                    if (info.zeroClampFlag ) ratio = Math.Clamp(ratio, 0.0f, 1.0f);
                    else ratio = Math.Clamp(ratio, 0.0f, 2.0f );

                    return ratio;
                }

                public static UInt32 GetDataRefAddressImpl(RefType refType, UInt32 value, UInt32 baseAddress)
                {
                    if (refType == RefType.REFTYPE_ADDRESS) return value;
                    else return baseAddress + value;
                }

            }
        }
    }
}
