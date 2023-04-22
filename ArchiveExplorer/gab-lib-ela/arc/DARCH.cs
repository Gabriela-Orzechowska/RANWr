using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using gablibela.io;
using static gablibela.arc.DARCH;

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
            public unsafe struct NodeData
            {
                public byte isDir;
                public fixed byte stringPool[3];
                public UInt32 DataStartOrParent;
                public UInt32 SizeOrEndNode;
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
                public UInt32 DataStart;
                public UInt32 DataSize;

                public UInt32 ParentIndex;
                public UInt32 EndNode;

                public List<Node> Children;
                public Node? Parent;

                public byte[] Data;

                public Node(NodeData data)
                {
                    this.Type = (NodeType)data.isDir;
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
            }

            public Header header;
            public string name;

            public NodeData[] rawNodeData;
            public Node[] rawNodes;
            public Node[] structure;

            public byte[] data;

            public DARCH(byte[] data) => new DARCH(data, "<null>.szs");

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

                UInt32 nodeCount = rootNodeData.SizeOrEndNode - 1;

                UInt32 stringTableOffset = header.NodeStart + rootNodeData.SizeOrEndNode * 0xC;
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
                rawNodes = _nodes.ToArray();

                structure = ConvertToStructure(rawNodes);
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

            private static Node[] ConvertToStructure(Node[] nodes)
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
                        {
                            
                            parentQueue.Remove(applyNode);
                        }
                    }
                }

                return result.ToArray();
            }

            private unsafe static UInt32 GetStringPoolOffset(NodeData node)
            {
                byte* p = node.stringPool;
                return (UInt32) (*p | *(p + 1) << 8 | *(p + 2) << 16);
            }
        }
    }
}
