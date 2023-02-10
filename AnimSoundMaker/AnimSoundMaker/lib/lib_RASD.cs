using AnimSoundMaker;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml;

public class lib_RASD
{
    public struct RASD
    {
        public NW4R_Xml_Header Header;
        public AnimSound AnimSound;

        public RASD()
        {
            Header = new();
            AnimSound = new();
        }
    }

    public struct NW4R_Xml_Header
    {
        public string CreatorName { get; set; }
        public string HostName { get; set; }
        public string DataSaved { get; set; }
        public string Title { get; set; }
        public string Generator { get; set; }
        public string GeneratorVersion { get; set; }
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

    public struct Event
    {
        public uint Index { get; set; }
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
    }

    public enum EventTypes
    {
        Range = 0,
        Trigger = 1
    }

    public static RASD? OpenRASP(string? filePath) {
        string? rasdFilePath = GetRASDFilepathFromRASP(filePath); return TryOpenRASD(rasdFilePath);
    }

    public static RASD? TryOpenRASD(string filePath)
    {
        if(string.IsNullOrEmpty(filePath)) return null;
        RASD _rasd = new RASD();
        XmlDocument doc = new XmlDocument();
        doc.Load(filePath);
        XmlElement root = doc.DocumentElement;

        if (root.Name != "banw_snd")
        {
            if (root.Name != "nintendoware_snd")
                return null;
        }

        XmlNode head = root.FirstChild;
        foreach (XmlNode n in head)
        {
            switch (n.Name)
            {
                case "create":
                    foreach (XmlAttribute attr in n.Attributes)
                    {
                        switch (attr.Name)
                        {
                            case "user":
                                _rasd.Header.CreatorName = attr.Value; break;
                            case "host":
                                _rasd.Header.HostName = attr.Value; break;
                            case "date":
                                _rasd.Header.DataSaved = attr.Value; break;
                        }
                    }
                    break;
                case "generator":
                    foreach (XmlAttribute attr in n.Attributes)
                    {
                        switch (attr.Name)
                        {
                            case "name":
                                _rasd.Header.Generator = attr.Value; break;
                            case "version":
                                _rasd.Header.GeneratorVersion = attr.Value; break;
                        }
                    }
                    break;
                case "title":
                    _rasd.Header.Title = n.InnerText; break;
            }
        }
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
                        _new.Volume = (int) Math.Floor(127 * float.Parse(_node.InnerText)); break;
                    case "pitch":
                        _new.Pitch = float.Parse(_node.InnerText); break;
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

        NW4R_Xml_Header header = new()
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


        for(int i = 0; i < eventCount; i++)
        {
            long currentOffset = stream.Position;
            Event _event = new();
            _event.Start = r.ReadUInt32(0x0);
            _event.End = r.ReadInt32(0x4);
            _event.PlaybackInterval = _event.End > -1;
            _event.Type = (r.ReadByte(0x08) & 0x1) != 0 ? EventTypes.Trigger : EventTypes.Range;
            _event.IntType = (int) _event.Type;
            _event.StringType = _event.Type.ToString();
            long soundDataOffset = r.ReadUInt32(0x10) + dataHeaderEnding;
            Debug.WriteLine(soundDataOffset);
            stream.Position = soundDataOffset;
            _event.Volume = r.ReadByte(0x10);
            _event.Pitch = r.ReadFloat(0x14);
            _event.Name = r.ReadStringNT(0x20);
            _event.UserParameter = r.ReadUInt32(0x1C);
            animSound.Events.Add(_event);
            stream.Position = currentOffset + 0x14;
        }

        _rasd.Header = header;
        _rasd.AnimSound = animSound;
        return _rasd;
    
    }




    //This won't be even called once in this apps lifetime lmao
    private static string? GetRASDFilepathFromRASP(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return null;
        XmlDocument doc = new XmlDocument();
        doc.Load(filePath);
        XmlElement root = doc.DocumentElement;

        if (root.Name != "banw_snd")
        {
            if (root.Name != "nintendoware_snd")
                return null;
        }
        XmlNode body = root.LastChild;

        XmlNode path = doc.SelectSingleNode("/banw_snd/body/anim_sound_project/model_array/model/anim_array/anim/anim_sound_array/anim_sound/file");
        if (path == null) path = doc.SelectSingleNode("/nintendoware_snd/body/anim_sound_project/model_array/model/anim_array/anim/anim_sound_array/anim_sound/file");
        if (path == null) return null;
        return Path.GetDirectoryName(filePath) + "\\" + path.Attributes[0].Value;

    }
}
