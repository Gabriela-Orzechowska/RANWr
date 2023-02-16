using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using static lib_NW4R;

public class lib_RASP
{
    public struct RASP
    {
        public Xml_Header Header;
        public AnimSoundProject Project;

        public RASP()
        {
            Header= new Xml_Header();
            Project= new AnimSoundProject();
        }
    }

    public struct AnimSoundProject
    {
        public List<string> BRSARFilepath;
        public List<RASPModelEntry> Models;

        public AnimSoundProject()
        {
            BRSARFilepath = new();
            Models = new();
        }
    }

    public struct RASPModelEntry
    {
        public string FilePath;
        public List<RASPAnimEntry> Anims;
    }
    public struct RASPAnimEntry
    {
        public string FilePath;
        public List<RASPSoundEntry> Sounds;
    }
    public struct RASPSoundEntry
    {
        public string FilePath;
    }


    public RASP? TryReadRASP(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return null;
        RASP _rasp = new();
        XmlDocument doc = new XmlDocument();
        doc.Load(filePath);
        XmlElement root = doc.DocumentElement;

        Xml_Header? xml_Header = TryReadHeader(doc);
        if (doc != null) _rasp.Header = xml_Header;

        XmlNode body = root.LastChild;
        XmlNode anim_project, sound_archive, project_settings, model_array;
        anim_project = sound_archive = project_settings = model_array = null;
        if (body != null) anim_project = body.FirstChild;
        if (anim_project == null) return null;
        foreach(XmlNode n in anim_project)
        {
            switch(n.Name)
            {
                case "sound_archive":
                    foreach(XmlNode node in n)
                    {
                        if (node.Name != "file") continue;
                        _rasp.Project.BRSARFilepath.Add(node.Attributes["path"].Value);
                    }
                    break;
                case "project_setting":
                    break; //TODO
                case "model_array":
                    model_array = n;
                    break;
            }
        }
        foreach(XmlNode n in model_array)
        {
            RASPModelEntry model = new();
            foreach(XmlNode m in n)
            {
                switch(m.Name)
                {
                    case "file":
                        model.FilePath = m.Attributes["path"].Value; break;
                    case "anim_array":
                        foreach(XmlNode a in m)
                        {
                            RASPAnimEntry anim = new();
                            switch (a.Name)
                            {
                                case "file":
                                    anim.FilePath = a.Attributes["path"].Value; break;
                                case "anim_sound_array":
                                    foreach(XmlNode s in a)
                                    {
                                        RASPSoundEntry sound = new();
                                        switch (s.Name)
                                        {
                                            case "file":
                                                sound.FilePath = s.Attributes["path"].Value; break;
                                        }
                                        anim.Sounds.Add(sound);
                                    }
                                    break;
                            }
                            model.Anims.Add(anim);
                        }
                        break;
                }
            }
            _rasp.Project.Models.Add(model);
        }

            
        return _rasp;
    }

}
