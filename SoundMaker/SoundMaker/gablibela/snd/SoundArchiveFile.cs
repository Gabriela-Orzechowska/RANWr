using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using gablibela.snd.detail;
using gablibela.ut;

using OffsetTable = gablibela.snd.detail.Util.Table<uint>;
using IdType = System.UInt32;


namespace gablibela
{ 
    namespace snd
    {
        namespace detail
        {
            class SoundArchiveFile
            {
                public const UInt16 FILE_VERSION = 0x0104;
                public const UInt16 FILE_VERSION_SOUND_COMMON_INFO_TABLE = 0x0101;
                public const UInt16 FILE_VERSION_SUPPORT_PAN_MODE = 0x0102;
                public const UInt16 FILE_VERSION_SUPPORT_RELEASE_PRIORITY_FIX = 0x0103;
                public const UInt16 FILE_VERSION_STRMSOUNDINFO_CHANNEL_COUNT = 0x0104;

                public enum SoundType
                {
                    SOUND_TYPE_INVALID = 0,
                    SOUND_TYPE_SEQ = 1,
                    SOUND_TYPE_STRM = 2,
                    SOUND_TYPE_WAVE = 3,
                };

                [StructLayout(LayoutKind.Explicit)]
                public struct StringTreeNode
                {
                    public const UInt16 FLAG_LEAF = (1 << 0);

                    [FieldOffset(0x00)] public UInt16 Flags;
                    [FieldOffset(0x02)] public UInt16 Bit;
                    [FieldOffset(0x04)] public UInt32 LeftIdx;
                    [FieldOffset(0x08)] public UInt32 EightIdx;
                    [FieldOffset(0x0C)] public UInt32 StrIdx;
                    [FieldOffset(0x10)] public UInt32 Id;
                }

                public class StringTree
                {
                    public UInt32 RootIdx;
                    Util.Table<StringTreeNode> NodeTable;
                }

                public class StringTable
                {
                    public OffsetTable offsetTable;
                }

                [StructLayout(LayoutKind.Explicit)]
                public class StringChunk
                {
                    [FieldOffset(0x00)] public UInt32 TableOffset;
                    [FieldOffset(0x04)] public UInt32 SoundTreeOffset;
                    [FieldOffset(0x08)] public UInt32 PlayerTreeOffset;
                    [FieldOffset(0x0C)] public UInt32 GroupTreeOffset;
                    [FieldOffset(0x10)] public UInt32 BankTreeOffset;
                }

                public struct StringUnionData
                {
                    public StringTable StringTable;
                    public StringChunk StringChunk;
                }

                public class StringBlock
                {
                    public StringUnionData u;
                }

                [StructLayout(LayoutKind.Explicit)]
                public class SymbolBlock
                {
                    [FieldOffset(0x00)] binaryFileFormats.BinaryBlockHeader BlockHeader;
                    [FieldOffset(0x12)] StringBlock StringBlock;
                }
                
                public class Sound3DParam
                {
                    public UInt32 Flags;
                    public byte DecayCurve;
                    public byte DecayRatio;
                    public byte DopplerFactor;
                    private byte Padding;
                    private UInt32 Reserved;
                }

                public class SoundInfo { }

                public class SeqSoundInfo : SoundInfo
                {
                    public UInt32 DataOffset;
                    public IdType BankId;
                    public UInt32 AllocTrack;
                    public byte ChannelPriority;
                    public byte ReleasePriorityFix;
                    private UInt16 Padding;
                    private UInt32 Reserved;
                }

                public class StrmSoundInfo : SoundInfo
                {
                    public UInt32 StartPosition;
                    public UInt16 AllocChannelCount;
                    public UInt16 AllocTrackFlag;
                    private UInt32 Reserved;
                }

                public class WaveSoundInfo : SoundInfo
                {
                    public Int32 SubNo;
                    public UInt32 AllocTrack;
                    public byte ChannelPriority;
                    public byte ReleasePriorityFix;
                    private UInt16 Padding;
                    private UInt32 Reserved;
                }

                public class SoundCommonInfo
                {
                    public IdType StringId;
                    public IdType FileId;
                    public IdType PlayerId;
                    public Util.DataRef param3dRef;
                    public byte Volume;
                    public byte PlayerPriority;
                    public byte SoundType;
                    public Util.DataRef soundInfoRef;
                    public UInt32 UserParam1;
                    public UInt32 UserParam2;
                    public byte PanMode;
                    public byte PanCurve;
                    public byte ActorPlayerId;
                    private byte Reserved;
                }
            }
        }
    }
}
