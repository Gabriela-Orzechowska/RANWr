using gablibela;
using gablibela.arc;
using gablibela.cmpr;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace ranwr
{
    public class arc
    {
        public static readonly string Version = "v0.2";
        public static readonly string ArcVersion = "v0.1";

        public static readonly string Title = $"RANWr: RANWr Ain't NintendoWare {Version} || Archive CLI Tool {ArcVersion}";

        public static int CompressionLevel = 10;

        public static void Main(string[] args)
        {
            if (args.Length == 0) noCommand();
            if(args.Length > 0)
            {
                if(args.Length > 2)
                {
                    switch (args[0])
                    {
                        case "c":
                        case "create":
                            byte type2 = 0;
                            for (int i = 1; i < args.Length; i++)
                            {
                                if (args[i].StartsWith("--lzma"))
                                {
                                    type2 = 1;
                                    args[i] = string.Empty;
                                }
                            }
                            create(args[1], args[2], type2);
                            break;

                        case "compress":
                            byte type = 0;
                            for(int i = 1; i < args.Length; i++)
                            {
                                type = 1;
                                args[i] = string.Empty;
                            }
                            compress(args[1], args[2], type);
                            break;
                        case "d":
                        case "decompress":
                            decompress(args[1], args[2]);
                            break;
                    }
                }
                else if (args.Length > 1)
                {
                    switch (args[0])
                    {
                        case "cmpr":
                        case "compress":
                            byte type = 0;
                            for (int i = 1; i < args.Length; i++)
                            {
                                if (args[i].StartsWith("--lzma"))
                                {
                                    type = 1;
                                    args[i] = string.Empty;
                                }
                            }
                            compress(args[1], null, type);
                            break;
                        case "d":
                        case "decompress":
                            decompress(args[1]);
                            break;
                        case "list":
                            listFiles(args[1]);
                            break;
                        case "x":
                        case "extract":
                            extract(args[1]);
                            break;
                        case "c":
                        case "create":
                            byte type2 = 0;
                            for (int i = 1; i < args.Length; i++)
                            {
                                type2 = 1;
                                args[i] = string.Empty;
                            }
                            create(args[1], null, type2);
                            break;
                    }
                    
                }
            }
        }

        public static void listFiles(string file)
        {
            byte[] original = File.ReadAllBytes(file);
            byte[] workFile = original;

            var signature = BitConverter.ToUInt32(workFile.Take(4).Reverse().ToArray());
            if (signature == YAZ0.SignatureHex) workFile = YAZ0.Decode(original);
            else if (LZMA.isLZMA(original)) workFile = LZMA.Decode(original);
            signature = BitConverter.ToUInt32(workFile.Take(4).Reverse().ToArray());
            if(signature == ARC.Signature)
            {
                ARC darch = new ARC(workFile, Path.GetFileName(file));
                Console.WriteLine($"{Title}\n");
                darch.PrintArchive();
            }
        }

        public static void testing()
        {
            Console.WriteLine(Title);
            Console.WriteLine($"> Type 'ranwr-arc -h' for help.\n");

            /*
            byte[] data = File.ReadAllBytes(@"C:\Users\Gabi\Pictures\RANWr\koopa_course.arc");
            DARCH darch = new DARCH(data);

            darch.AddNode("test1", DARCH.Node.NodeType.Directory, "");
            darch.AddNode("test2.txt", DARCH.Node.NodeType.File, new byte[16], "test1");
            darch.RemoveNode("b_teresa.brres");

            byte[] _exportData = darch.Encode();
            File.WriteAllBytes(@"C:\Users\Gabi\Pictures\RANWr\export_test.arc", _exportData);

            darch.FreeArchive();
            */
            create(@"C:\Users\Gabi\Documents\GitHub\BANW\ArchiveExplorer\ranwr-arc\bin\Debug\net7.0\koopa_course.d");
        }

        public static void noCommand()
        {
            Console.WriteLine(Title);
            Console.WriteLine($"> Type 'ranwr-arc -h' for help.");
        }

        public static void extract(string file, string output = null)
        {
            if (!Path.Exists(file))
            {
                fileNotFound(file);
                return;
            }

            if (output == null) output = Path.GetFileNameWithoutExtension(file) + ".d";
            Directory.CreateDirectory(output);
            ARC darch = GetArchive(file);
            darch.SetFolder(output);
            darch.ExportAllNodes();

        }

        public static void create(string file, string output = null, byte type = 0)
        {
            Console.WriteLine($"Creating: {file} type: {type}");
            if (!Path.Exists(file))
            {
                fileNotFound(file);
                return;
            }
            if(!File.GetAttributes(file).HasFlag(FileAttributes.Directory))
            {
                return;
            }

            if (output == null || output == string.Empty)
            {
                if(type == 1) output = Path.GetFileNameWithoutExtension(file) + ".lzma";
                else output = Path.GetFileNameWithoutExtension(file) + ".szs";
            }
            ARC darch = new();
            darch.SetFolder(file);
            darch.UpdateAllNodeData();
            byte[] data = new byte[1];
            if(Path.GetExtension(output.ToLower()) == ".szs") data = YAZ0.Compress(darch.EncodeARC());
            else if (Path.GetExtension(output.ToLower()) == ".lzma") data = LZMA.Encode(darch.EncodeARC());

            File.WriteAllBytes(output, data);
        }
 
        public static void compress(string file, string output = null, byte type = 0)
        {
            if (output == null || output == string.Empty) output = Path.GetFileNameWithoutExtension(file) + ".szs";

            Console.WriteLine(Title);
            Console.WriteLine($"Compressing: {Path.GetFileName(file)} to {output}");

            byte[] original = File.ReadAllBytes(file);
            var signature = BitConverter.ToUInt32(original.Take(4).Reverse().ToArray());
            if (signature == YAZ0.SignatureHex)
            {
                original = YAZ0.Decode(original);
            }
            else if (LZMA.isLZMA(original))
            {
                original = LZMA.Decode(original);
            }
            byte[] compressed = new byte[1];
            if (type == 0) compressed = YAZ0.Compress(original);
            else if (type == 1) compressed = LZMA.Encode(original);

            File.WriteAllBytes(output, compressed);
        }

        public static void decompress(string file, string output = null)
        {
            if (output == null) output = Path.GetFileNameWithoutExtension(file) + ".arc";

            Console.WriteLine(Title);
            Console.WriteLine($"Decompressing: {Path.GetFileName(file)} to {output}");

            byte[] original = File.ReadAllBytes(file);
            var signature = BitConverter.ToUInt32(original.Take(4).Reverse().ToArray());
            byte[] decompressed = new byte[1];
            if (signature == YAZ0.SignatureHex)
                decompressed = YAZ0.Decode(original);
            else if (LZMA.isLZMA(original))
                decompressed = LZMA.Decode(original);
            else return;
            File.WriteAllBytes(output, decompressed);
        }

        public static void fileNotFound(string path)
        {
            Console.WriteLine($"Could not find the file: {path}");
        }

        public static ARC GetArchive(string path) => GetArchive(File.ReadAllBytes(path));

        public static ARC GetArchive(byte[] data)
        {
            var signature = BitConverter.ToUInt32(data.Take(4).Reverse().ToArray());
            if (signature == YAZ0.SignatureHex)
            {
                data = YAZ0.Decode(data);
                signature = BitConverter.ToUInt32(data.Take(4).Reverse().ToArray());
            }
            else if (LZMA.isLZMA(data))
            {
                data = LZMA.Decode(data);
                signature = BitConverter.ToUInt32(data.Take(4).Reverse().ToArray());
            }
            if (signature != ARC.Signature) return null;

            return new(data);
        }

    }
    
}