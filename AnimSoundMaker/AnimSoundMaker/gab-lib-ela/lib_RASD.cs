using AnimSoundMaker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using static lib_NW4R;

public class lib_RASD
{
    public class RASD : ClassBase
    {
        public Xml_Header Header;
        public AnimSound AnimSound;
        public string FilePath;

        public RASD()
        {
            Header = new();
            AnimSound = new();
        }
    }

    public struct AnimSound
    {
        public uint FrameSize { get; set; }
        public List<Event> Events { get; set; }

        public AnimSound()
        {
            FrameSize= 0;
            Events = new List<Event>();
        }
    }

    public class ClassBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            var handler = PropertyChanged;
            handler?.Invoke(this, args);
        }
    }

    public class Event : ClassBase
    {
        public uint Index;
        public uint Start { get; set; }
        public int End { get; set; }
        [DisplayName("Playback Interval")]
        public bool PlaybackInterval { get; set; }
        public EventTypes Type { get; set; }
        public int IntType;
        public string StringType;
        public string Name { get; set; }
        public float Pitch { get; set; }
        public int Volume { get; set; }
        public uint UserParameter { get; set; }
        public string Comment { get; set; }

        public Event()
        {
            Index = 0;
            Start = 0;
            End = -1;
            PlaybackInterval = false;
            Type = EventTypes.Trigger;
            IntType = 1;
            StringType = "Trigger";
            Name = "";
            Pitch = 1;
            Volume = 127;
            UserParameter= 0;
            Comment = "";
        }

    }

    public enum EventTypes
    {
        Range = 0,
        Trigger = 1
    }


    public static RASD? TryOpenRASD(string filePath)
    {
        if(string.IsNullOrEmpty(filePath)) return null;
        RASD _rasd = new RASD();
        _rasd.FilePath = filePath;
        XmlDocument doc = new XmlDocument();
        doc.Load(filePath);
        XmlElement root = doc.DocumentElement;

        Xml_Header? xml_Header = TryReadHeader(doc);
        if (doc != null) _rasd.Header = xml_Header;

        XmlNode body = root.LastChild;
        XmlNode anim_sound = null;
        XmlNode event_array = null;
        int event_count = 0;
        foreach (XmlNode n in body)
        {
            if(n.Name == "anim_sound")
                anim_sound = n; break;
        }
        if (anim_sound == null) return null;
        foreach(XmlNode n in anim_sound)
        {
            switch (n.Name)
            {
                case "frame_size":
                    _rasd.AnimSound.FrameSize = uint.Parse(n.InnerText); break;
                case "event_array":
                    event_array = n;
                    event_count = int.Parse(n.Attributes[0].Value); break;
            }
        }
        uint i = 0;
        foreach (XmlNode _event in event_array)
        {
            if (_event.Name != "event") continue;
            Event _new = new();
            _new.End = -1;
            _new.Index = i;
            foreach (XmlNode _node in _event)
            {
                _new.Comment = "";
                switch(_node.Name)
                {
                    case "frame":
                        _new.Type = _node.Attributes[0].Value == "trigger" ? EventTypes.Trigger : EventTypes.Range;
                        _new.IntType = (int)_new.Type;
                        _new.StringType = _new.Type.ToString();
                        foreach(XmlNode _frame in _node)
                        {
                            switch(_frame.Name)
                            {
                                case "start":
                                    _new.Start = uint.Parse(_frame.InnerText); break;
                                case "end":
                                    _new.End = int.Parse(_frame.InnerText);
                                    _new.PlaybackInterval = _new.End > -1;  break;
                            }
                        }
                        i++;
                        break;
                    case "sound":
                        foreach(XmlNode _sound in _node)
                        {
                            switch(_sound.Name)
                            {
                                case "id":
                                    _new.Name = _sound.InnerText; break;
                            }
                        }
                        break;
                    case "comment":
                        _new.Comment = _node.InnerText; break;
                    case "volume":
                        float volume = float.Parse(_node.InnerText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture);
                        _new.Volume = (int) Math.Round(127 * volume); break;
                    case "pitch":
                        _new.Pitch = float.Parse(_node.InnerText.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture); break;
                    case "user_param":
                        _new.UserParameter = uint.Parse(_node.InnerText); break;

                }
            }
            _rasd.AnimSound.Events.Add(_new);
        }
        
        return _rasd;
    }

    public static RASD? TryOpenBRASD(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return null;
        FileStream stream = File.OpenRead(filePath);
        BigEndianReader r = new(stream);
        if (r.ReadString(4, 0x0) != "RASD") return null;
        RASD _rasd = new();
        _rasd.FilePath = filePath;
        Xml_Header header = new()
        {
            CreatorName = "None",
            HostName = "None",
            DataSaved = File.GetLastWriteTime(filePath).ToString(),
            Title = Path.GetFileNameWithoutExtension(filePath),
            Generator = "None",
            GeneratorVersion = r.ReadUInt16(0x06).ToString()
        };

        stream.Position = r.ReadUInt32(0x10) + 0x8;
        long dataHeaderEnding = stream.Position;
        AnimSound animSound = new();
        animSound.FrameSize = r.ReadUInt32(0x0);
        uint eventCount = r.ReadUInt32(0x0C);
        stream.Position += 0x10;
        Debug.WriteLine(stream.Position);


        for(uint i = 0; i < eventCount; i++)
        {
            long currentOffset = stream.Position;
            Event _event = new();
            _event.Index = i;
            _event.Start = r.ReadUInt32(0x0);
            _event.End = r.ReadInt32(0x4);
            _event.PlaybackInterval = _event.End > -1;
            _event.Type = (r.ReadByte(0x08) & 0x1) != 0 ? EventTypes.Trigger : EventTypes.Range;
            _event.IntType = (int) _event.Type;
            _event.StringType = _event.Type.ToString();
            long soundDataOffset = r.ReadUInt32(0x10) + dataHeaderEnding;
            Debug.WriteLine(soundDataOffset);
            stream.Position = soundDataOffset;
            _event.Volume = Math.Clamp(r.ReadByte(0x10),(byte)0,(byte)127);
            _event.Pitch = r.ReadFloat(0x14);
            _event.Name = r.ReadStringNT(0x20);
            _event.UserParameter = r.ReadUInt32(0x1C);
            _event.Comment = "";
            animSound.Events.Add(_event);
            stream.Position = currentOffset + 0x14;
        }

        _rasd.Header = header;
        _rasd.AnimSound = animSound;
        stream.Close();
        return _rasd;
    
    }

    public static void SaveRASD(string filepath, RASD data)
    {
        XElement[] events = new XElement[data.AnimSound.Events.Count];
        for (int i = 0; i < data.AnimSound.Events.Count; i++)
        {
            XElement frame;
            XElement sound;
            Event @event = data.AnimSound.Events[i];
            if (@event.End != -1)
            {
                frame = new XElement("frame", new XAttribute("type", @event.Type.ToString().ToLower()),
                                            new XElement("start", new XAttribute("type", "frame"), @event.Start.ToString()),
                                            new XElement("end", new XAttribute("type", "frame"), @event.End.ToString()));
            }
            else
            {
                frame = new XElement("frame", new XAttribute("type", @event.Type.ToString().ToLower()),
                                            new XElement("start", new XAttribute("type", "frame"), @event.Start.ToString()));
            }
            sound = new XElement("sound",
                new XElement("id", new XAttribute("type", "name"), @event.Name));

            XElement xem = new XElement("event", frame, sound, new XElement("volume", (((float)@event.Volume) / 127f).ToString()), new XElement("pitch", @event.Pitch.ToString()),
                new XElement("comment", @event.Comment), new XElement("user_param", @event.UserParameter));

            events[i] = xem;
            
        }

        XElement head = new XElement("head",
            new XElement("create", new XAttribute("user", Environment.UserName), new XAttribute("host", Environment.MachineName), new XAttribute("date", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")), new XAttribute("source", "")),
            new XElement("title", data.Header.Title), new XElement("generator", new XAttribute("name", "RANWR AnimSoundMaker"), new XAttribute("version","1.0")));

        XElement body = new XElement("body",
            new XElement("anim_sound", new XElement("frame_size", data.AnimSound.FrameSize), new XElement("event_array", new XAttribute("size", data.AnimSound.Events.Count), events)));

        XElement main = new XElement("ranwr_snd", new XAttribute("version", "1.0.0"), new XAttribute("platform", "Revolution"), head, body);
        
        XDocument xdoc = new XDocument(new XDeclaration("1.0",null,null),main);
        using (StreamWriter file = new(filepath, false))
        {
            xdoc.Save(file);
        }

    }   

    public static void SaveBRASD(string filepath, RASD data)
    {
        BigEndianWriter writer = new BigEndianWriter(File.Open(filepath, FileMode.Create));

        ulong dataOffset = 0x20;
        ulong eventRelative = dataOffset + 0x8;

        //Write Header
        writer.Write("RASD", 0x00);
        writer.Write((UInt16)0xFEFF, 0x04);
        writer.Write((UInt16)0x0100, 0x06);
        //Skip Lenght for now
        writer.Write((UInt16)0x20, 0x0C);
        writer.Write((UInt16)0x01, 0x0E);
        writer.Write((UInt32)0x20, 0x10);
        //Skip Lenght for now;

        //Write Data Header
        writer.Write("DATA", dataOffset + 0x00);
        //Skip Lenght for now

        //Write Event Header
        Event[] events = data.AnimSound.Events.ToArray();
        writer.Write((UInt32)data.AnimSound.FrameSize, eventRelative + 0x00);
        writer.Write((UInt32)0x01000000, eventRelative + 0x04);
        writer.Write((UInt32)0x0000000C, eventRelative + 0x08);
        writer.Write((UInt32)events.Length, eventRelative + 0x0C);

        //Write Frame Events
        for(int i = 0; i < events.Length; i++)
        {
            Event @event = events[i];
            ulong eventFrameOffset = eventRelative + (ulong)(0x10 + i * 0x14);
            writer.Write((UInt32)@event.Start, eventFrameOffset + 0x00);
            writer.Write((Int32)@event.End, eventFrameOffset + 0x04);
            writer.Write((byte)ConvertFlagsToByte(@event), eventFrameOffset + 0x08);
            writer.Write((byte)0x1, eventFrameOffset + 0x0A); //Unknown; put placeholder
            writer.Write((UInt32)0x01000000, eventFrameOffset + 0x0C); //Unknown; put placeholder
            //Skip Lenght for now
        }

        ulong soundEventPosition = eventRelative + (ulong)(0x10 + events.Length * 0x14);
        for (int i = 0; i < events.Length; i++)
        {
            Event @event = events[i];
            ulong eventFrameOffset = eventRelative + (ulong)(0x10 + i * 0x14);
            ulong soundOffset = eventFrameOffset + 0x10;
            ulong soundEvent = soundEventPosition - eventRelative;

            writer.Write((UInt32)soundEvent, soundOffset);

            WriteSoundEvent(writer, @event, soundEventPosition, out ulong lenght);
            soundEventPosition += lenght;
        }
        ulong streamLenght = (ulong)writer.Length();
        ulong streamEnd = (ulong) Math.Ceiling((float)streamLenght / 16) * 16;
        Debug.WriteLine(streamLenght);
        Debug.WriteLine(streamEnd);
        //Fill to end
        writer.Write((byte)0x0, streamEnd - 1);

        //Fill Lenghts
        writer.Write((UInt32)streamEnd, 0x08);
        ulong dataSectionLenght = streamEnd - dataOffset;

        writer.Write((UInt32)dataSectionLenght, dataOffset+0x4);
        writer.Write((UInt32)dataSectionLenght, 0x14);


        writer.CloseStream();
    }

    private static void WriteSoundEvent(BigEndianWriter writer, Event @event, ulong start, out ulong lenght, bool roundTo0x04 = true)
    {
        writer.Write((UInt32)0x00000000, start + 0x00);
        writer.Write((UInt32)0xFFFFFFFF, start + 0x04);
        writer.Write((UInt32)0x01000000, start + 0x08);
        writer.Write((UInt32)0x00000020, start + 0x0C);
        byte volume = (byte)@event.Volume;
        if (volume == 127) volume++;
        writer.Write(volume, start+ 0x10);
        writer.Write(new byte[3], start + 0x11);
        writer.Write((float)@event.Pitch, start + 0x14);
        writer.Write((UInt32)0x00000000, start + 0x18);
        writer.Write((UInt32)@event.UserParameter, start + 0x1C);

        string name = @event.Name;
        int nameLenght = name.Length + 1;
        writer.Write(name, start + 0x20);
        writer.Write((byte)0x0, start + 0x20 + (ulong)name.Length);
        if (roundTo0x04)
            nameLenght = (int) Math.Ceiling((float)nameLenght / 4) * 4;
        lenght = 0x20 + (ulong)nameLenght;

    } 

    private static byte ConvertFlagsToByte(Event @event)
    {
        byte result = 0;
        if (@event.Type == EventTypes.Trigger) result |= 0x1;
        if(@event.End == -1) result |= 0x2;
        return result;
    }
    
}
