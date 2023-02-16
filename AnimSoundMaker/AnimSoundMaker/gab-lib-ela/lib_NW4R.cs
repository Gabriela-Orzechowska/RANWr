using System.Xml;

public class lib_NW4R
{
    public class Xml_Header
    {
        public string CreatorName { get; set; }
        public string HostName { get; set; }
        public string DataSaved { get; set; }
        public string Title { get; set; }
        public string Generator { get; set; }
        public string GeneratorVersion { get; set; }
    }

    public static Xml_Header? TryReadHeader(XmlDocument doc)
    {
        XmlElement root = doc.DocumentElement;
        Xml_Header header = new Xml_Header();

        if (root.Name != "ranwr_snd")
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
                                header.CreatorName = attr.Value; break;
                            case "host":
                                header.HostName = attr.Value; break;
                            case "date":
                                header.DataSaved = attr.Value; break;
                        }
                    }
                    break;
                case "generator":
                    foreach (XmlAttribute attr in n.Attributes)
                    {
                        switch (attr.Name)
                        {
                            case "name":
                                header.Generator = attr.Value; break;
                            case "version":
                                header.GeneratorVersion = attr.Value; break;
                        }
                    }
                    break;
                case "title":
                    header.Title = n.InnerText; break;
            }
        }
        return header;
    }


}

