using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using gablibela.io;

namespace gablibela
{
    namespace arc
    {
        public class DARCH
        {
            public static readonly UInt32 Signature = 0x55AA382D; //DARCH Signature

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct Header
            {
                public UInt32 Signature;
                public UInt32 NodeStart;
                public UInt32 NodeSize;
                public UInt32 DataStart;
                public fixed UInt32 Reserved[4];
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct NodeData
            {
                public UInt32 TypeAndOffset;
                public Int32 DataStartOrParent;
                public Int32 SizeOrEndNode;
            }

            public class Node
            {
                public enum NodeType
                {
                    File = 0,
                    Directory = 1,
                    OptionCount,
                }

                public NodeType Type;
                public int Index;
                public UInt32 StringPoolOffset;
                public string Name;
                public Int32 DataStart;
                public Int32 DataSize;

                public Int32 ParentIndex;
                public Int32 EndNode;

                public List<Node> Children;
                public Node? Parent;

                public byte[] Data;

                public Node(NodeData data)
                {
                    this.Type = (NodeType)(data.TypeAndOffset >> 24);
                    if (Type == NodeType.File)
                    {
                        this.DataStart = data.DataStartOrParent;
                        this.DataSize = data.SizeOrEndNode;
                    }
                    else
                    {
                        this.ParentIndex = data.DataStartOrParent;
                        this.EndNode = data.SizeOrEndNode;
                    }
                    Children = new List<Node>();
                }
                public Node(string name, byte[] data, NodeType type)
                {
                    Name = name;
                    Data = data;
                    Type = type;
                    DataSize = data.Length;
                    Children = new();
                }
            }

            public Header header;
            public string name;

            public NodeData[] rawNodeData;
            public List<Node> rawNodes;
            public Node structure;
            public byte[] data;
            public string TemporaryPath;

            public DARCH(byte[] data) : this(data, "untitled.arc") { }

            public DARCH(byte[] _data, string fileName)
            {
                this.data = _data;
                this.name = fileName;
                MemoryStream stream = new MemoryStream(data);
                BetterBinaryReader reader = new BetterBinaryReader(stream);
                header = reader.ReadStruct<Header>(0x00);

                List<NodeData> _nodeData = new List<NodeData>();
                List<Node> _nodes = new List<Node>();

                NodeData rootNodeData = reader.ReadStruct<NodeData>(header.NodeStart);
                Node rootNode = new Node(rootNodeData);

                Int32 nodeCount = rootNodeData.SizeOrEndNode - 1;

                Int64 stringTableOffset = header.NodeStart + rootNodeData.SizeOrEndNode * 0xC;
                rootNode.Name = reader.ReadStringNT(stringTableOffset + GetStringPoolOffset(rootNodeData));

                _nodeData.Add(rootNodeData);
                _nodes.Add(rootNode);

                for (int i = 0; i < nodeCount; i++)
                {
                    NodeData nodeRawData = reader.ReadStruct<NodeData>(header.NodeStart + (i+1) * 0xC);
                    Node node = new Node(nodeRawData);
                    node.Name = reader.ReadStringNT(stringTableOffset + GetStringPoolOffset(nodeRawData));
                    node.Index = i + 1;
                    _nodeData.Add(nodeRawData);
                    _nodes.Add(node);
                    if(node.Type == Node.NodeType.File)
                        node.Data = reader.ReadBytes((int)nodeRawData.SizeOrEndNode, nodeRawData.DataStartOrParent);
                }
                rawNodeData = _nodeData.ToArray();
                rawNodes = _nodes;

                structure = ConvertToStructure(rawNodes.ToArray());
                RecalculateStructureIndexes();
                TemporaryPath = GetTemporaryDirectory();
            }

            public DARCH()
            {
                rawNodeData = new NodeData[0];
                Node struc = new("", new byte[0], Node.NodeType.Directory);
                struc.Name = "";
                rawNodes = new() { struc };
                structure = struc;
                TemporaryPath = GetTemporaryDirectory();
            }

            public void PrintArchive()
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine($"Archive: {name}\n");
                Console.ResetColor();

                Console.WriteLine("   size   | SIGN | file/directory");
                Console.WriteLine("---------------------------");

                foreach(var node in rawNodes)
                {
                    if (node.Name == "") continue;

                    string path = GetNodePath(node);
                    string sizeorsmth = node.Type == Node.NodeType.Directory ? "-----" : node.DataSize.ToString();
                    string magic = ".DIR";
                    if(node.Type == Node.NodeType.File) magic = Encoding.UTF8.GetString(node.Data.Take(4).ToArray());
                    magic = magic.Replace('\0', '.');
                    Console.WriteLine($"{sizeorsmth.PadLeft(10)}  {magic.PadLeft(4)}  {path}");
                }
            }

            private static Node ConvertToStructure(Node[] nodes)
            {
                List<Node> result = new();
                List<Node> parentQueue = new();

                foreach(Node node in nodes) 
                {
                    if (node.Type == Node.NodeType.Directory)
                    {
                        if (parentQueue.Count != 0)
                        {     
                            parentQueue.Last().Children.Add(node);
                            node.Parent = parentQueue.Last();
                        }
                        else result.Add(node);
                        parentQueue.Add(node);                
                    }
                    else
                    {
                        parentQueue.Last().Children.Add(node);
                        node.Parent = parentQueue.Last();
                    }

                    if (parentQueue.Any(f => f.EndNode == node.Index + 1))
                    {
                        Node[] apply = parentQueue.FindAll(f => f.EndNode == node.Index + 1).ToArray();
                        apply.Reverse();
                        foreach (Node applyNode in apply) 
                            parentQueue.Remove(applyNode);
                    }
                }
                if (result[0].Children.Count > 0)
                {
                    if (result[0].Children[0].Name == "." && result[0].Children[0].Type == Node.NodeType.Directory) result = result[0].Children;
                }
                result[0].Name = "";
                result[0].Parent = null;
                return result.ToArray()[0];
            }

            private static Int32 GetStringPoolOffset(NodeData node) => (Int32) node.TypeAndOffset & 0x00FFFFFF;

            public void RecalculateStructureIndexes()
            {
                int start = 1;
                RecalculateNodeIndexes(ref start, structure.Children.ToArray());
                structure.Children = structure.Children.OrderBy(f => f.Index).ToList();
                RecalculateDirectoryLastNode(rawNodes.ToArray());
            }

            private void RecalculateNodeIndexes(ref int start, Node[] nodes)
            {
                nodes = nodes.OrderBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Type).ToArray();
                foreach(var node in nodes)
                {
                    node.Index = start;
                    start++;
                    if(node.Children.Count > 0)
                    {
                        node.Children = node.Children.OrderBy(x => x.Name, StringComparer.Ordinal).ThenBy(x => x.Type).ToList();
                        RecalculateNodeIndexes(ref start, node.Children.ToArray());
                        node.Children = node.Children.OrderBy(n => n.Index).ToList();
                    }
                }
                
                rawNodes = rawNodes.OrderBy(n => n.Index).ToList();
            }

            private static void RecalculateDirectoryLastNode(Node[] nodes)
            {
                foreach(var node in nodes)
                {
                    if (node.Type == Node.NodeType.Directory) node.EndNode = GetTheLastOfUs(node).Index+1;
                }
            }

            private static Node GetTheLastOfUs(Node node)
            {
                if (node.Children.Count > 0)
                {
                    var _new = GetTheLastOfUs(node.Children.MaxBy(n => n.Index));
                    return _new;
                }
                else return node;
            }

            public Node GetNodeByName(string name)
            {
                Node returnNode = rawNodes.FirstOrDefault(n => n.Name == name);
                return returnNode;
            }

            public void UpdateAllNodeData()
            {
                List<string> allfiles = Directory.GetFileSystemEntries(TemporaryPath, "*", SearchOption.AllDirectories).ToList();
                allfiles = allfiles.Order().ToList();

                foreach(var file in allfiles)
                {
                    string fullPath = file;
                    string relative = Path.GetRelativePath(TemporaryPath, file);
                    Node node = null;
                    try
                    {
                        node = GetNodeByPath(relative);
                        if (node == structure || node == null) throw new Exception();
                        if (relative != GetNodePath(node)) throw new Exception();
                        UpdateNodeData(node);
                    } 
                    catch(Exception e)
                    {
                        FileAttributes attr = File.GetAttributes(fullPath);
                        Node.NodeType type = attr.HasFlag(FileAttributes.Directory) ? Node.NodeType.Directory : Node.NodeType.File;
                        string name = Path.GetFileName(file);
                        byte[] data = new byte[0];
                        if (type == Node.NodeType.File) data = File.ReadAllBytes(fullPath);
                        AddNode(name, type, data, Path.GetDirectoryName(relative));
                    }
                }
                RecalculateStructureIndexes();
            }
            public Node GetNodeByPath(string path)
            {
                path = path.Replace("\\", "/");
                string[] nodeNames = path.Split("/");
                Node node = structure;
                foreach (var _name in nodeNames)
                {
                    if (node.Children.Find(x => x.Name == _name) != null) node = node.Children.First(x => x.Name == _name);
                }
                return node;
            }


            public void UpdateNodeData(Node node)
            {
                string nodePath = GetNodePath(node);
                string exportPath = PathCombine(TemporaryPath, nodePath);
                if (!Path.Exists(exportPath)) throw new Exception();
                if(node == null) throw new Exception();
                if (node.Type == Node.NodeType.File)
                {
                    node.Data = File.ReadAllBytes(exportPath);
                    node.DataSize = node.Data.Length;
                }
            }

            public void AddNode(string name, Node.NodeType type, string path) => AddNode(name, type, Array.Empty<byte>(), path);

            public void AddNode(string name, Node.NodeType type, byte[] data, string path)
            {
                Node node = GetNodeByPath(path);
                AddNode(name, type, data, node);
            }

            public void AddNode(string name, Node.NodeType type, Node parent) => AddNode(name, type, Array.Empty<byte>(), parent);

            public Node AddNode(string name, Node.NodeType type, byte[] data, Node parent)
            {
                if (parent.Type != Node.NodeType.Directory) throw new Exception("Parent Node can't be a file");
                Node node = new(name,data,type);
                node.Parent = parent;
                parent.Children.Add(node);
                rawNodes.Add(node);
                RecalculateStructureIndexes();
                return node;
            }

            public bool CheckIfNodeIsExported(Node node)
            {
                string nodePath = GetNodePath(node);
                string exportPath = PathCombine(TemporaryPath, nodePath);
                return Path.Exists(exportPath);
            }

            public string RenameNode(Node node, string newName)
            {
                string nodePath = GetNodePath(node);
                string exportPath = PathCombine(TemporaryPath, nodePath);
                string newPath = Path.Combine(Path.GetDirectoryName(exportPath), newName);

                if (Path.GetExtension(newName) == "")
                {
                    newName += Path.GetExtension(nodePath);
                    newPath += Path.GetExtension(nodePath);
                }

                if (Path.Exists(newPath))
                {
                    string originalNewPath = newPath;
                    newPath = PathCombine(Path.GetDirectoryName(newPath), Path.GetFileNameWithoutExtension(newPath));
                    newPath += " (1)";
                    newPath += Path.GetExtension(originalNewPath);

                    string originalNewName = newName;
                    newName = Path.GetFileNameWithoutExtension(newName);
                    newName += " (1)";
                    newName += Path.GetExtension(originalNewName);
                }


                if (CheckIfNodeIsExported(node))
                {
                    if(node.Type == Node.NodeType.File) File.Move(exportPath, newPath);
                    else Directory.Move(exportPath, newPath);
                }
                node.Name = newName;
                RecalculateStructureIndexes();
                Node str = structure;
                return newName;
            }

            public void RemoveNode(string path)
            {
                string[] nodeNames = path.Split("/");
                Node node = structure;
                foreach(var name in nodeNames)
                {
                    if(node.Children.Find(x => x.Name == name) != null) node = node.Children.First(x => x.Name == name);
                }
                RemoveNode(node);
            }

            public void RemoveNode(Node node)
            {
                if(node.Parent != null) node.Parent.Children.Remove(node);
                rawNodes.Remove(node);
                RecalculateStructureIndexes();
            }

            public string GetNodePath(Node node)
            {
                List<string> names = new List<string>();
                Node testNode = node;
                if(testNode == null)
                {
                    names.Insert(0,"");
                    return string.Join("\\", names.ToArray());
                }
                while (testNode.Parent != null)
                {
                    names.Insert(0, testNode.Name);
                    testNode = testNode.Parent;
                }
                names.Append(node.Name);
                return string.Join("\\", names.ToArray());
            }

            public void OpenNode(Node node)
            {
                string nodePath = GetNodePath(node);
                string exportPath = PathCombine(TemporaryPath, nodePath);
                var p = new Process();
                p.StartInfo = new ProcessStartInfo(exportPath)
                {
                    UseShellExecute = true
                };
                p.Start();
            }

            public void ExportAndOpenNode(Node node)
            {
                ExportNode(node);
                OpenNode(node);
            }

            public void ExportNode(Node node)
            {
                string nodePath = GetNodePath(node);
                string exportPath = PathCombine(TemporaryPath, nodePath);
                if (node.Type == Node.NodeType.File)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
                    File.WriteAllBytes(exportPath, node.Data);
                }
            }

            private string PathCombine(string path1, string path2)
            {
                if (Path.IsPathRooted(path2))
                {
                    path2 = path2.TrimStart(Path.DirectorySeparatorChar);
                    path2 = path2.TrimStart(Path.AltDirectorySeparatorChar);
                }

                return Path.Combine(path1, path2);
            }

            public void ExportAllNodes()
            {
                foreach(Node node in rawNodes)
                {
                    if(node.Type == Node.NodeType.File) ExportNode(node);
                }
            }

            public static string GetTemporaryDirectory()
            {
                string tempDirectory = Path.Combine(Path.GetTempPath(), "RANWr" , Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);
                return tempDirectory;
            }

            public void SetFolder(string path) => TemporaryPath = path;

            public string ComputeStringPool(out List<int> offsets)
            {
                offsets = new();
                int i = 0;
                string output = "";

                foreach(var node in rawNodes)
                {
                    var nodeName = node.Name + '\0';
                    output += nodeName;
                    offsets.Add(i);
                    i += nodeName.Length;
                }
                return output;
            }

            public void FreeArchive()
            {
                data = Array.Empty<byte>();
                Directory.Delete(TemporaryPath, true);
            }

            public byte[] Encode()
            {
                MemoryStream stream= new MemoryStream();
                BetterBinaryWriter writer = new BetterBinaryWriter(stream);

                List<int> stringPoolOffsets = new();
                string stringPool = ComputeStringPool(out stringPoolOffsets);

                File.WriteAllBytes(@"C:\Users\Gabi\Pictures\RANWr\stringpool.bin", Encoding.ASCII.GetBytes(stringPool));

                UInt32 fstLenght = (UInt32) (rawNodes.Count * 0xC + stringPool.Length);
                UInt32 dataOffset = roundTo32(fstLenght + 0x20);

                //Header
                writer.Write(0x55AA382D);
                writer.Write((Int32)0x20);
                writer.Write(fstLenght);
                writer.Write(dataOffset);
                writer.Write(new byte[16]);

                int i = 0;
                uint currentDataOffset = dataOffset;
                List<uint> fileOffsets = new List<uint> { currentDataOffset };
                //Nodes
                foreach (var node in rawNodes)
                {
                    writer.Write(((byte)node.Type << 24) + stringPoolOffsets[i]); i++;
                    if(node.Type == Node.NodeType.File)
                    {
                        writer.Write(currentDataOffset);
                        currentDataOffset += roundTo32((uint)node.DataSize);
                        fileOffsets.Add(currentDataOffset);
                        writer.Write(node.DataSize);
                    }
                    else
                    {
                        int index = 0;
                        if (node.Parent != null) index = node.Parent.Index;
                        writer.Write(index);
                        writer.Write(node.EndNode);
                    }
                    
                }

                writer.Write(stringPool);
                writer.Seek(0x00);
                i = 0;
                foreach(var node in rawNodes)
                {
                    if(node.Type == Node.NodeType.File)
                    {
                        writer.Write(node.Data, fileOffsets[i]); i++;
                    }
                }

                //Cleanup
                long FileLenght = writer.Length();
                int finishUp = (int)(roundTo32((uint)FileLenght) - FileLenght);
                writer.Write(new byte[finishUp], (ulong)FileLenght);


                return stream.ToArray();
            }

            private static UInt32 roundTo32(UInt32 x) => (UInt32)Math.Ceiling((float)x / 0x20) * 0x20;

        }
    }
}
