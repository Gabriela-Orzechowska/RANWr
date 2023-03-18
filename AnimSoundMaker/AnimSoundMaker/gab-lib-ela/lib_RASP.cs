using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Xml;
using static AnimSoundMaker.MainWindow;
using gablibela;
using static gablibela.lib_RASD;

namespace gablibela
{
    public class lib_RASP
    {
        public struct RASP
        {
            public lib_NW4R.Xml_Header Header;
            public AnimSoundProject Project;

            public RASP()
            {
                Header = new lib_NW4R.Xml_Header();
                Project = new AnimSoundProject();
            }
        }

        public struct AnimSoundProject
        {
            public List<string> BRSARFilepath;
            public string Name;
            public List<RASPModelEntry> Models;

            public AnimSoundProject()
            {
                BRSARFilepath = new();
                Models = new();
                Name = string.Empty;
            }
        }

        public struct RASPModelEntry
        {
            public string FilePath;
            public string Name;
            public List<RASPAnimEntry> Anims;

            public RASPModelEntry()
            {
                FilePath = string.Empty;
                Name = string.Empty;
                Anims = new();
            }
        }
        public struct RASPAnimEntry
        {
            public string FilePath;
            public string Name;
            public List<RASPSoundEntry> Sounds;

            public RASPAnimEntry()
            {
                FilePath = string.Empty;
                Name = string.Empty;
                Sounds = new();
            }
        }
        public struct RASPSoundEntry
        {
            public string FilePath;
            public string Name;
            public RASD Data;

            public RASPSoundEntry()
            {
                FilePath = string.Empty;
                Name = string.Empty;
                Data = new();
            }
        }


        public static RASP? TryReadRASP(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            RASP _rasp = new();
            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            XmlElement root = doc.DocumentElement;

            lib_NW4R.Xml_Header? xml_Header = lib_NW4R.TryReadHeader(doc);
            if (doc != null) _rasp.Header = xml_Header;

            _rasp.Project.Name = Path.GetFileNameWithoutExtension(filePath);

            XmlNode body = root.LastChild;
            XmlNode anim_project, sound_archive, project_settings, model_array;
            anim_project = sound_archive = project_settings = model_array = null;
            if (body != null) anim_project = body.FirstChild;
            if (anim_project == null) return null;
            foreach (XmlNode n in anim_project)
            {
                switch (n.Name)
                {
                    case "sound_archive":
                        foreach (XmlNode node in n)
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
            foreach (XmlNode n in model_array)
            {
                RASPModelEntry model = new();
                foreach (XmlNode m in n)
                {
                    switch (m.Name)
                    {
                        case "file":
                            model.FilePath = m.Attributes["path"].Value;
                            model.Name = Path.GetFileNameWithoutExtension(model.FilePath);
                            break;
                        case "anim_array":
                            foreach (XmlNode a in m)
                            {
                                RASPAnimEntry anim = new();
                                foreach (XmlNode an in a)
                                {


                                    switch (an.Name)
                                    {
                                        case "file":
                                            anim.FilePath = Path.GetDirectoryName(filePath) + @"\" + an.Attributes["path"].Value;
                                            anim.Name = Path.GetFileNameWithoutExtension(anim.FilePath);
                                            break;
                                        case "anim_sound_array":
                                            foreach (XmlNode s in an)
                                            {
                                                RASPSoundEntry sound = new();
                                                foreach (XmlNode sn in s)
                                                {
                                                    switch (sn.Name)
                                                    {
                                                        case "file":
                                                            sound.FilePath = Path.GetDirectoryName(filePath) + @"\" + sn.Attributes["path"].Value;
                                                            sound.Data = TryOpenRASD(sound.FilePath);
                                                            sound.Name = Path.GetFileNameWithoutExtension(sound.FilePath);
                                                            break;

                                                    }
                                                    anim.Sounds.Add(sound);
                                                }

                                            }
                                            break;
                                    }

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

        public static RASP ProjectFromRASD(string name, RASD rasd, string folderName = "null_model")
        {
            RASPSoundEntry sound = new();
            sound.Data = rasd;
            sound.Name = name;
            sound.FilePath = rasd.Header.Title;

            RASPAnimEntry anim = new();
            anim.Name = name;
            anim.Sounds.Add(sound);

            RASPModelEntry model = new();
            model.Name = folderName;
            model.Anims.Add(anim);

            RASP rasp = new();
            rasp.Project.Name = "<null-project>";
            rasp.Project.Models.Add(model);

            return rasp;
        }

        public static RASP AddSoundToProject(string name, string modelName, string animName, RASD rasd, RASP rasp)
        {
            foreach (var model in rasp.Project.Models)
            {
                if (model.Name != modelName) continue;
                foreach (var anim in model.Anims)
                {
                    if (anim.Name != animName) continue;
                    RASPSoundEntry sound = new();
                    sound.Name = name;
                    sound.Data = rasd;
                    anim.Sounds.Add(sound);
                }

            }
            return rasp;
        }

        public static RASP AddSoundToProject(TreeViewItem itAnim, string name, RASD rasd, RASP rasp, string? modelName = null)
        {
            bool hadModelName = modelName != null;
            char[] model_header = "M_".ToCharArray();
            if (!hadModelName)

                modelName = (itAnim.Parent as TreeViewItem).Header.ToString().TrimStart(model_header);

            if (!hadModelName)
            {
                foreach (var model in rasp.Project.Models)
                {
                    Debug.WriteLine(model.Name);
                    Debug.WriteLine(modelName);
                    if (model.Name != modelName) continue;

                    RASPSoundEntry sound = new();
                    sound.Name = name;
                    sound.Data = rasd;

                    RASPAnimEntry anim = new();
                    anim.Name = name;
                    anim.Sounds.Add(sound);

                    model.Anims.Add(anim);
                }
            }
            else
            {
                RASPModelEntry model = new();
                RASPSoundEntry sound = new();
                sound.Name = name;
                sound.Data = rasd;

                RASPAnimEntry anim = new();
                anim.Name = name;
                anim.Sounds.Add(sound);


                RASPModelEntry m = rasp.Project.Models.Cast<RASPModelEntry>().FirstOrDefault(m => m.Name == modelName);
                if (m.Name != null)
                {
                    model = (RASPModelEntry)m;
                    model.Anims.Add(anim);
                }

                else
                {
                    model.Name = modelName;
                    model.Anims.Add(anim);
                    rasp.Project.Models.Add(model);
                }

            }
            Debug.WriteLine(rasp);
            return rasp;
        }

        public static RASP RemoveSoundFromProject(TreeViewItem itSound, RASP rasp)
        {
            TreeViewItem itAnim = itSound.Parent as TreeViewItem;
            TreeViewItem itModel = itAnim.Parent as TreeViewItem;
            TreeViewItem project = itModel.Parent as TreeViewItem;
            RASPModelEntry? emptyModel = null;
            foreach (var model in rasp.Project.Models)
            {
                if ("M_" + model.Name == itModel.Name)
                {
                    foreach (var anim in model.Anims)
                    {
                        if ("A_" + anim.Name == itAnim.Name)
                        {
                            model.Anims.Remove(anim);
                            break;
                        }
                    }
                }
                if (model.Anims.Count == 0) emptyModel = model;
            }
            if (emptyModel != null)
            {
                rasp.Project.Models.Remove((RASPModelEntry)emptyModel);
            }
            return rasp;
        }

    }
}
