using System;
using System.IO;
using System.Xml;


public class xmlToFile
{
    private const string filename = "C:\\Users\\Gabi\\Downloads\\sip\\riivolution\\Skill Issue Pack.xml";
    private const string referenceFolder = "C:\\Users\\Gabi\\Documents\\Dolphin Emulator\\Load\\WiiSDSync";
    private const string mainPatch = "loadpack";

    string output = "";

    public static void Main(string[] args)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(filename);

        XmlNode wiidisc = doc.DocumentElement.SelectSingleNode("/wiidisc");

        foreach(XmlNode node in wiidisc.ChildNodes)
        {
            if(node.Name == "patch")
            {
                if (node.Attributes["id"].Value == mainPatch)
                {
                    foreach(XmlNode patch in node.ChildNodes)
                    {
                       
                        if(patch.Name == "folder")
                        {
                            string disc = patch.Attributes["disc"].Value;
                            string external = patch.Attributes["external"].Value.Substring(1);
                            string refFolder = Path.Combine(referenceFolder, external);

                            if (disc == "/") disc = "";

                            DirectoryInfo info  = new DirectoryInfo(refFolder);

                            foreach (var file in info.GetFiles("*"))
                            {
                                Console.WriteLine("    { \"" + disc + "/" + file.Name + "\", \"sd:/" + external + "/" + file.Name + "\" }, ");
                            }
                        }
                    }
                }
            }
        }
    }
}