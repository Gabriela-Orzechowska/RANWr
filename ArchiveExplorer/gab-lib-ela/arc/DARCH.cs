using System.Runtime.InteropServices;
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

            public DARCH(byte[] data) : this(data, "<null>.szs") { }

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

                    Node testNode = node;
                    List<string> names = new List<string>();
                    while(testNode.Parent != null)
                    {
                        names.Insert(0, testNode.Name);
                        testNode = testNode.Parent;
                    }
                    names.Append(node.Name);
                    string sizeorsmth = node.Type == Node.NodeType.Directory ? "-----" : node.DataSize.ToString();
                    string magic = ".DIR";
                    if(node.Type == Node.NodeType.File) magic = Encoding.UTF8.GetString(node.Data.Take(4).ToArray());
                    magic = magic.Replace('\0', '.');
                    Console.WriteLine($"{sizeorsmth.PadLeft(10)}  {magic.PadLeft(4)}  {string.Join("\\", names.ToArray())}");
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

                return result.ToArray()[0];
            }

            private static Int32 GetStringPoolOffset(NodeData node) => (Int32) node.TypeAndOffset & 0x00FFFFFF;

            public void RecalculateStructureIndexes()
            {
                int start = 1;
                RecalculateNodeIndexes(ref start, structure.Children.ToArray());
                RecalculateDirectoryLastNode(rawNodes.ToArray());
            }

            private void RecalculateNodeIndexes(ref int start, Node[] nodes)
            {
                foreach(var node in nodes)
                {
                    node.Index = start;
                    start++;
                    if(node.Children.Count > 0)
                    {
                        node.Children = node.Children.OrderBy(x => x.Name).OrderBy(x=>x.Type).ToList();
                        RecalculateNodeIndexes(ref start, node.Children.ToArray()) ;
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
                    var _new = GetTheLastOfUs(node.Children.Last());
                    return _new;
                }
                else return node;
            }

            public Node GetNodeByName(string name)
            {
                Node returnNode = rawNodes.FirstOrDefault(n => n.Name == name);
                return returnNode;
            }

            public void AddNode(string name, Node.NodeType type, byte[] data, string path)
            {
                string[] nodeNames = path.Split("/");
                Node node = structure;
                foreach (var _name in nodeNames)
                {
                    if (node.Children.Find(x => x.Name == _name) != null) node = node.Children.First(x => x.Name == _name);
                }
                AddNode(name, type, data, node);
            }

            public void AddNode(string name, Node.NodeType type, byte[] data, Node parent)
            {
                if (parent.Type != Node.NodeType.Directory) throw new Exception("Parent Node can't be a file");
                Node node = new(name,data,type);
                node.Parent = parent;
                parent.Children.Add(node);
                rawNodes.Add(node);
                RecalculateStructureIndexes();
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

        }
    }
}
