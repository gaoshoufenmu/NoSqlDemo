using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using HanLP.csharp.seg.CRF;
using HanLP.csharp.collection.trie;
using HanLP.csharp.corpus.tag;

namespace QZ.Redis.Search
{

    public class HuffmanTree
    {
        private List<Node> nodes = new List<Node>();
        public Node Root { get; set; }
        public Dictionary<char, int> Frequencies = new Dictionary<char, int>();

        private static HuffmanTree _tree;
        public static HuffmanTree Tree
        {
            get
            {
                if (_tree == null)//9860
                {
                    _tree = new HuffmanTree();
                    _tree.Build("SHIXIHIOM455689566778843990011HELLOWORLDORFUCKWORLDWHOKNOWS2233442355566778899ABCDEFGHIJKLMNOPQRSTUVWXYZ000000111122334798U965555");
                }
                return _tree;
            }
        }

        public void Build(string source)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (!Frequencies.ContainsKey(source[i]))
                {
                    Frequencies.Add(source[i], 0);
                }

                Frequencies[source[i]]++;
            }

            foreach (KeyValuePair<char, int> symbol in Frequencies)
            {
                nodes.Add(new Node() { Symbol = symbol.Key, Frequency = symbol.Value });
            }

            while (nodes.Count > 1)
            {
                List<Node> orderedNodes = nodes.OrderBy(node => node.Frequency).ToList<Node>();

                if (orderedNodes.Count >= 2)
                {
                    // Take first two items
                    List<Node> taken = orderedNodes.Take(2).ToList<Node>();

                    // Create a parent node by combining the frequencies
                    Node parent = new Node()
                    {
                        Symbol = '*',
                        Frequency = taken[0].Frequency + taken[1].Frequency,
                        Left = taken[0],
                        Right = taken[1]
                    };

                    nodes.Remove(taken[0]);
                    nodes.Remove(taken[1]);
                    nodes.Add(parent);
                }

                this.Root = nodes.FirstOrDefault();

            }

        }

        public string Encode2Str(string source)
        {
            List<bool> encodedSource = new List<bool>();

            for (int i = 0; i < source.Length; i++)
            {
                List<bool> encodedSymbol = this.Root.Traverse(source[i], new List<bool>());
                encodedSource.AddRange(encodedSymbol);
            }


            List<byte> ret = new List<byte>();
            int count = 0;
            byte currentByte = 0;

            long sum = 0;

            foreach (bool b in encodedSource)
            {

                if (b)
                    currentByte |= (byte)(1 << count);

                count++;
                if (count == 8)
                {
                    ret.Add(currentByte);
                    sum += currentByte;
                    currentByte = 0;
                    count = 0;
                }
            }

            if (count < 8)
            {
                ret.Add(currentByte);
                sum += currentByte;
            }

            var tail = sum % 100;

            return Util.ByteToHex(ret.ToArray()) + tail.ToString("D2");
        }

        public string Decode(string input)
        {
            var len = input.Length;
            var bytes = Util.HexToByte(input.Substring(0, len - 2));
            return Decode(new BitArray(bytes));
        }

        

        public BitArray Encode(string source)
        {
            List<bool> encodedSource = new List<bool>();

            for (int i = 0; i < source.Length; i++)
            {
                List<bool> encodedSymbol = this.Root.Traverse(source[i], new List<bool>());
                encodedSource.AddRange(encodedSymbol);
            }

            BitArray bits = new BitArray(encodedSource.ToArray());
            
            return bits;
        }

        public string Decode(BitArray bits)
        {
            Node current = this.Root;
            string decoded = "";

            foreach (bool bit in bits)
            {
                if (bit)
                {
                    if (current.Right != null)
                    {
                        current = current.Right;
                    }
                }
                else
                {
                    if (current.Left != null)
                    {
                        current = current.Left;
                    }
                }

                if (IsLeaf(current))
                {
                    decoded += current.Symbol;
                    current = this.Root;
                }
            }

            return decoded;
        }

        public bool IsLeaf(Node node)
        {
            return (node.Left == null && node.Right == null);
        }


        
    }


    public class Node
    {
        public char Symbol { get; set; }
        public int Frequency { get; set; }
        public Node Right { get; set; }
        public Node Left { get; set; }

        public List<bool> Traverse(char symbol, List<bool> data)
        {
            // Leaf
            if (Right == null && Left == null)
            {
                if (symbol.Equals(this.Symbol))
                {
                    return data;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                List<bool> left = null;
                List<bool> right = null;

                if (Left != null)
                {
                    List<bool> leftPath = new List<bool>();
                    leftPath.AddRange(data);
                    leftPath.Add(false);

                    left = Left.Traverse(symbol, leftPath);
                }

                if (Right != null)
                {
                    List<bool> rightPath = new List<bool>();
                    rightPath.AddRange(data);
                    rightPath.Add(true);
                    right = Right.Traverse(symbol, rightPath);
                }

                if (left != null)
                {
                    return left;
                }
                else
                {
                    return right;
                }
            }
        }

    }






    /**
     * Huffman Tree Implementation Version 2
     * */


    /// <summary>
    /// 编码器，使用Huffman编码，主要用于公司名数据压缩
    /// </summary>
    public class HuffmanEncoder
    {
        /// <summary>
        /// text -> code
        /// </summary>
        private DoubleArrayTrie<string> _dat = new DoubleArrayTrie<string>();
        /// <summary>
        /// code -> text
        /// </summary>
        private DoubleArrayTrie<string> _reDat = new DoubleArrayTrie<string>();

        public HuffmanEncoder(HuffmanTree2 tree)
        {
            _dat.Build(tree.table);
            _reDat.Build(tree.reTable);
        }



        public byte[] Encode(string com_name)
        {
            var terms = Com_CRFSegment.Segment(com_name);
            var sb = new StringBuilder(com_name.Length * 8);
            byte count = 0;
            var flags = new List<char>();
            for (int i = 0; i < terms.Count; i++)
            {
                var term = terms[i];
                if (term.nc == NatCom.E || term.nc == NatCom.W || term.word.Any(c => c < 128))
                {
                    count += (byte)term.word.Length;
                    foreach (var c in term.word)
                    {
                        sb.Append(Convert.ToString(c, 2).PadLeft(16, '0'));
                        flags.Add('0');
                    }
                }
                else
                {
                    var val = _dat.GetOrDefault(term.word);
                    if (val == null)
                    {
                        count += (byte)term.word.Length;
                        foreach (var c in term.word)
                        {
                            var temp_s = Convert.ToString(c, 2).PadLeft(16, '0');
                            sb.Append(temp_s/*.Substring(16)*/);
                            flags.Add('0');
                        }
                    }
                    else
                    {
                        count++;
                        sb.Append(val);
                        flags.Add('1');
                    }
                }
            }
            if (count > 63)
                throw new Exception("Length of company name is larger than 63");

            // word 数量固定占用6bit，故word数量不能超过63
            // 前缀 bit 数
            //var prefix_len = count + 6;
            var prefix_str = Convert.ToString(count, 2).PadLeft(6, '0') + new string(flags.ToArray());


            // 所有的数据二进制的字符串形式
            var data_str = prefix_str + sb.ToString();
            // 缺少的 bit 数，填充为 8 的整数倍
            var lack = data_str.Length % 8 == 0 ? 0 : 8 - (data_str.Length % 8);
            for (int j = 0; j < lack; j++)
                data_str += "0";

            var bs = new byte[data_str.Length / 8];
            for (int j = 0; j < bs.Length; j++)
                bs[j] = Convert.ToByte(data_str.Substring(j * 8, 8), 2);
            return bs;
        }

        public string Decode(byte[] bs)
        {
            var sb = new StringBuilder(bs.Length * 8);
            for (int i = 0; i < bs.Length; i++)
                sb.Append(Convert.ToString(bs[i], 2).PadLeft(8, '0'));
            var data_str = sb.ToString();
            sb.Clear();
            var count = Convert.ToByte(data_str.Substring(0, 6), 2);
            // 正文部分解码的位置
            var cursor = count + 6;
            for (int i = 0; i < count; i++)
            {
                if (data_str[6 + i] == '1')        // huffman解码
                {
                    var t = _reDat.GetShortestPrefix(data_str, cursor, data_str.Length);
                    if (t != null)
                    {
                        cursor += t.Item1;
                        sb.Append(t.Item2);
                    }
                    else
                        throw new Exception("Huffman decode error");
                }
                else                                // 普通解码
                {
                    var us = Convert.ToUInt16(data_str.Substring(cursor, 16), 2);
                    cursor += 16;
                    var c = (char)us;
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
    public class HuffmanTree2
    {
        /// <summary>
        /// key: keyword
        /// value: coressponding 0-1 string stream
        /// </summary>
        public SortedDictionary<string, string> table = new SortedDictionary<string, string>();

        public SortedDictionary<string, string> reTable = new SortedDictionary<string, string>();


        public HuffmanNode Root;
        public void Encode(HuffmanNodeList hnl)
        {
            while (hnl.Count > 1)
            {
                var arr = hnl.PopTwo();
                var left = arr[0];
                var right = arr[1];
                left.PathCode = 0;
                right.PathCode = 1;
                var parent = new HuffmanNode(left.Freq + right.Freq, left, right);
                left.Parent = parent;
                right.Parent = parent;
                hnl.Add(parent);
            }
            Root = hnl.GetFirst();

            for (int i = 0; i < hnl.Leaves.Length; i++)
            {
                var node = hnl.Leaves[i];
                var key = node.Word;
                var cs = new List<char>();
                while (node.Parent != null)
                {
                    cs.Add(node.PathCode == 0 ? '0' : '1');
                    node = node.Parent;
                }
                var chars = new char[cs.Count];
                for (int j = cs.Count - 1; j >= 0; j--)
                {
                    chars[cs.Count - 1 - j] = cs[j];
                }
                var code = new string(chars);
                table[key] = code;
                reTable[code] = key;
            }
        }


        public void Save2Txt(string file)
        {
            var lines = new string[table.Count];
            int i = 0;
            foreach (var p in table)
            {
                lines[i++] = $"{p.Key} {p.Value}";
            }
            File.WriteAllLines(file.Replace(".txt", "_f.txt"), lines);
        }
        public void LoadTxt(string file)
        {
            foreach (var line in File.ReadLines(file))
            {
                var segs = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                table[segs[0]] = segs[1];
                reTable[segs[1]] = segs[0];
            }
        }

        public static string Decode()
        {
            return null;
        }

        public static HuffmanTree2 Create(string file)
        {
            var tree = new HuffmanTree2();
            var path = file.Substring(file.Length - 4) + "_huffman.txt";
            if (File.Exists(path))
            {
                tree.LoadTxt(path);
            }
            else
            {
                tree.Encode(HuffmanNodeList.Create(file));
                tree.Save2Txt(file);
            }
            return tree;
        }

        protected static string ReadBitwise(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath, "*.bin");
            //Open .bin certain file
            FileStream readStream = new FileStream(files[0], FileMode.Open);
            BinaryReader readBinary = new BinaryReader(readStream);
            //Save Content of file as a decimal bytes array
            byte[] decimalByte = readBinary.ReadBytes(Convert.ToInt32(readStream.Length));
            readStream.Close();
            string[] binaryArray = new string[decimalByte.Length];
            int counter = 0;
            foreach (var item in decimalByte)
            {
                //converting decimal array to a binary array as string
                binaryArray[counter++] = Convert.ToString(item, 2).PadLeft(8, '0');
            }
            StringBuilder s = new StringBuilder();
            foreach (var item in binaryArray)
            {
                //Attaching string array Elements to Create a string
                s.Append(item);
            }
            return s.ToString();
        }
    }

    /// <summary>
    /// 构造huffman树的专用列表
    /// </summary>
    public class HuffmanNodeList
    {
        private LinkedList<HuffmanNode> nodes;
        public HuffmanNode[] Leaves;

        public HuffmanNode GetFirst() => nodes.First.Value;

        /// <summary>
        /// 节点数量
        /// </summary>
        public int Count => nodes.Count;

        /// <summary>
        /// 给定一组排序的叶子节点构造节点列表
        /// </summary>
        /// <param name="leaves">按频率从小到大排序的叶子节点数组</param>
        public HuffmanNodeList(HuffmanNode[] leaves)
        {
            Leaves = leaves;
            nodes = new LinkedList<HuffmanNode>();
            for (int i = 0; i < leaves.Length; i++)
            {
                nodes.AddLast(leaves[i]);
            }
        }

        public void Add(HuffmanNode node)
        {
            var n = nodes.First;
            while (n != null && n.Value.Freq < node.Freq)
            {
                n = n.Next;
            }
            if (n == null)
                nodes.AddLast(node);
            else
                nodes.AddBefore(n, node);
        }

        public HuffmanNode[] PopTwo()
        {
            if (nodes.Count > 1)
            {
                var arr = new HuffmanNode[2];
                arr[0] = nodes.First.Value;
                nodes.RemoveFirst();
                arr[1] = nodes.First.Value;
                nodes.RemoveFirst();
                return arr;
            }
            return null;
        }

        public static HuffmanNodeList Create(string file)
        {
            var ls = new List<HuffmanNode>();
            foreach (var line in File.ReadAllLines(file))
            {
                var segs = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                ls.Add(new HuffmanNode(segs[0], int.Parse(segs[1])));
            }
            var nodes = ls.ToArray();
            SortAscendByFreq(nodes);
            return new HuffmanNodeList(nodes);
        }

        public static void SortAscendByFreq(HuffmanNode[] nodes)
        {
            for (int i = 1; i < nodes.Length; i++)
            {
                if (nodes[i - 1].Freq > nodes[i].Freq)
                {
                    var temp = nodes[i];
                    int j = i;
                    while (j > 0 && nodes[j - 1].Freq > temp.Freq)
                    {
                        nodes[j] = nodes[j - 1];
                        j--;
                    }
                    nodes[j] = temp;
                }
            }
        }
    }

    public class HuffmanNode : IComparable
    {
        public string Word { get; private set; }
        public int Freq { get; private set; }

        public HuffmanNode Left;
        public HuffmanNode Right;
        public HuffmanNode Parent;
        public int PathCode { get; set; }
        public HuffmanNode()
        { }

        public HuffmanNode(string word, int freq)
        {
            Word = word;
            Freq = freq;
        }

        public HuffmanNode(int freq, HuffmanNode left, HuffmanNode right)
        {
            Freq = freq;
            Left = left;
            Right = right;
        }

        public int CompareTo(object obj)
        {
            var that = obj as HuffmanNode;
            if (that != null)
            {
                if (Freq == that.Freq) return 0;
                if (Freq > that.Freq) return 1;
                else return -1;
            }
            throw new ArgumentException("object is not a valid Huffman node");
        }

        public bool IsLeaf() => Left == null && Right == null;
    }

    /// <summary>
    /// 字母与数字组成的字符串的编码器，只对 9 位机构代码进行编码
    /// 由于认为所有字符等概率出现，所以也可认为是一种huffman tree，只不过所有叶子节点在同一深度
    /// </summary>
    public class AlnumEncoder
    {
        /// <summary>
        /// 将9位机构代码编码为一个long数值类型
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static long ToLong(string input)
        {
            // 编码原理： 0-9 -> 0-9, A-Z -> 10-35
            long l = 0;
            foreach (var c in input)
            {
                var v = c >= 'A' ? c - 'A' + 10 : c - '0';
                l = (l << 6) | v;
            }
            return l;
        }

        /// <summary>
        /// 将long数值转为9位机构代码
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public static string ToStr(long l)
        {
            var sb = new StringBuilder(10);
            var chunk = 0x3F;
            for (int i = 8; i >= 0; i--)
            {
                var v = (int)((l >> (6 * i)) & chunk);
                sb.Append((char)(v < 10 ? '0' + v : 'A' + v - 10));
            }
            return sb.ToString();
        }
    }
}
