using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static lib_NW4R;

public class lib_RASP
{
    public struct RASP
    {
        public Xml_Header Header;
        public AnimSoundProject Project;
    }

    public struct AnimSoundProject
    {
        public string BRSARFilepath;
        public List<string> ModelFilepaths;
        public List<string> AnimFilepaths;
        public List<string> RASDFilepaths;
    }
}
