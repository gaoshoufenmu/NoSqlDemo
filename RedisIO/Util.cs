using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QZ.Redis.Search
{
    public class Util
    {
        public static HashSet<string> Fanbingbing_Codes = new HashSet<string>()
        {
            "346354850",
            "677429191",
            "066035150",
            "302132201",
            "574862101",
            "559455364"
        };

        public static HashSet<string> Zhangchuanmei_Codes = new HashSet<string>()
        {
            "677429191",
            "58577051X",
            "MA00GW2C2",
            "072368680",
            "58577051x",
            ""
        };

        // 060424980

        public static string ToHexStr(string input)
        {
            char[] chars = input.ToCharArray();
            var sb = new StringBuilder();
            foreach (char c in chars)
            {
                // Get the integral value of the character.
                int value = Convert.ToInt32(c);

                //var hexStr = Convert.ToString(value, 16);
                sb.Append(string.Format("{0:x}", value));
            }
            return sb.ToString();
        }

        public static string FromHexStr(string input)
        {
            if (input.Length % 2 != 0) return null;

            var sb = new StringBuilder();
            for (int i = 0; i < input.Length; i += 2)
            {
                var seg = input.Substring(i, 2);
                var c = (char)Convert.ToInt32(seg, 16);
                sb.Append(c);
            }
            return sb.ToString();
        }


        public static string ByteToHex(byte[] vByte)
        {
            if (vByte == null || vByte.Length < 1) return null;
            var sb = new StringBuilder(vByte.Length * 2);
            for (int i = 0; i < vByte.Length; i++)
            {
                if ((UInt32)vByte[i] < 0) return null;
                UInt32 k = (UInt32)vByte[i] / 16;
                sb.Append((Char)(k + ((k > 9) ? 'A' - 10 : '0')));
                k = (UInt32)vByte[i] % 16;
                sb.Append((Char)(k + ((k > 9) ? 'A' - 10 : '0')));
            }
            return sb.ToString();
        }

        static public byte[] HexToByte(string szHex)
        {
            // 两个十六进制代表一个字节  
            Int32 iLen = szHex.Length;
            if (iLen <= 0 || 0 != iLen % 2)
            {
                return null;
            }
            Int32 dwCount = iLen / 2;
            UInt32 tmp1, tmp2;
            Byte[] pbBuffer = new Byte[dwCount];
            for (Int32 i = 0; i < dwCount; i++)
            {
                tmp1 = (UInt32)szHex[i * 2] - (((UInt32)szHex[i * 2] >= (UInt32)'A') ? (UInt32)'A' - 10 : (UInt32)'0');
                if (tmp1 >= 16) return null;
                tmp2 = (UInt32)szHex[i * 2 + 1] - (((UInt32)szHex[i * 2 + 1] >= (UInt32)'A') ? (UInt32)'A' - 10 : (UInt32)'0');
                if (tmp2 >= 16) return null;
                pbBuffer[i] = (Byte)(tmp1 * 16 + tmp2);
            }
            return pbBuffer;
        }

        /// <summary>
        /// 为指定公司聚合相关的一组公司
        /// </summary>
        /// <param name="ents"></param>
        /// <param name="self">指定的公司</param>
        /// <returns></returns>
        public static List<EntPartedGroup> ClusterForCom(List<EntPartedFull> ents, EntPartedFull self)
        {
            var group = new EntPartedGroup(self);
            var reminds = new List<EntPartedFull>();

            bool fanbingbing_flag = false;
            if(Fanbingbing_Codes.Contains(group.cluster[0].code))
            {
                fanbingbing_flag = true;
            }

            for (int i = 0; i < ents.Count; i++)
            {
                if (ents[i].isSelf) continue;
                //var curRelation = group.GetRelation(ents[i]);   // 当前企业与当前分组的相关度
                //if(curRelation > 1)
                //{
                //    group.Add(ents[i]);
                //}
                //else
                //{
                //    reminds.Add(ents[i]);
                //}

                // *****************  手动处理范冰冰 **************************
                if(fanbingbing_flag && Fanbingbing_Codes.Contains(ents[i].code))
                {
                    group.Add(ents[i]);
                    continue;
                }
                // **********************************************************

                if (group.IsRelated(ents[i]))
                {
                    group.Add(ents[i]);
                }
                else
                    reminds.Add(ents[i]);
            }
            for(int i = 0; i < reminds.Count; i++)
            {
                //if (group.GetRelation(reminds[i]) > 1)
                //    group.Add(reminds[i]);

                // *****************  手动处理范冰冰 **************************
                if (fanbingbing_flag && Fanbingbing_Codes.Contains(reminds[i].code))
                {
                    group.Add(reminds[i]);
                    continue;
                }
                // *****************  手动处理范冰冰 **************************

                if (group.IsRelated(reminds[i]))
                    group.Add(reminds[i]);
            }
            return new List<EntPartedGroup>() { group };
        }

        /// <summary>
        /// 公司聚类
        /// 根据公司与分组之间的相同名称数量，名称包括成员，股东以及对外投资
        /// </summary>
        /// <param name="ents"></param>
        /// <returns></returns>
        public static List<EntPartedGroup> Cluster(List<EntPartedFull> ents)
        {
            var groups = new List<EntPartedGroup>();

            for (int i = 0; i < ents.Count; i++)
            {
                var cur = ents[i];          // 当前企业
                var maxRelation = -1;          // 最大相关度
                var maxIdx = -1;
                for (int j = 0; j < groups.Count; j++)      // 遍历当前的分组，为了找出与当前企业相关度最大的分组
                {
                    var group = groups[j];                  // 当前分组
                    var curRelation = group.GetRelation(cur);   // 当前企业与当前分组的相关度
                    if (maxRelation == -1 || maxRelation < curRelation)   // 记录最大相关度，以及最大相关度所在的分组index
                    {
                        maxRelation = curRelation;
                        maxIdx = j;
                    }
                }
                if (maxIdx == -1 || maxRelation < 2)    // 如果没有找出最大相关度的分组，则当前企业自成一个分组
                {
                    groups.Add(new EntPartedGroup(cur));
                }
                else
                {
                    groups[maxIdx].Add(cur);
                }
            }
            return groups;
        }

        /// <summary>
        /// Optimized clustering
        /// </summary>
        /// <param name="ents"></param>
        /// <returns></returns>
        public static List<EntPartedGroup> Cluster_OP(List<EntPartedFull> ents, int reGroupLimit = 1024)
        {
            var nextGroups = new List<EntPartedGroup>();
            var groups1 = new List<EntPartedGroup>();   // 1类分组包含多个企业
            var groups2 = new List<EntPartedGroup>();   // 2类分组包含一个企业
            List<EntPartedFull> reminds = null;

            for (int c = 0; c < 2; c++)
            {
                reminds = new List<EntPartedFull>();

                for (int i = 0; i < ents.Count; i++)             // 遍历编号：(0)
                {
                    var cur = ents[i];          // 当前企业

                    if(cur.name == "哈尔滨新媒体文化产业集团有限公司")
                    {

                    }

                    bool added = false;         // 当前企业是否已经添加到某个分组中
                    #region calc relation
                    for(int j = 0; j < groups1.Count; j++)
                    {
                        var group = groups1[j];          // 当前分组
                        int relation = 0;               // 当前分组与当前公司的相关度
                        foreach (var s in cur.total)
                        {
                            if (group.names.Contains(s))
                            {
                                relation++;
                                if (relation == 2) break;
                            }
                        }
                        if (relation == 2)       // 当前分组与当前公司相关，则添加到分组中
                        {
                            group.Add(cur);
                            added = true;
                            break;              // 跳出遍历 (1)
                        }
                    }
                    if (added) continue;

                    int idx = -1;
                    for (int j = 0; j < groups2.Count; j++)      // 遍历编号：(1)
                    {
                        var group = groups2[j];          // 当前分组
                        int relation = 0;               // 当前分组与当前公司的相关度
                        foreach (var s in cur.total)
                        {
                            if (group.names.Contains(s))
                            {
                                relation++;
                                if (relation == 2) break;
                            }
                        }
                        if (relation == 2)       // 当前分组与当前公司相关，则添加到分组中
                        {
                            group.Add(cur);
                            idx = j;
                            //groups1.Add(group);
                            break;              // 跳出遍历 (1)
                        }
                        // 否则，对下一个分组进行相关测试
                    }
                    if(idx >= 0)
                    {
                        for (int j = 0; j < groups1.Count; j++)
                        {
                            var group = groups1[j];          // 当前分组
                            int relation = 0;               // 当前分组与当前公司的相关度
                            foreach (var s in groups2[idx].names)
                            {
                                if (group.names.Contains(s))
                                {
                                    relation++;
                                    if (relation == 2) break;
                                }
                            }
                            if (relation == 2)       // 分组2中某个分组合并到分组1中某个分组
                            {

                                //group.Add(groups2[idx].cluster[0]);
                                group.Extend(groups2[idx]);         // merge

                                added = true;
                                break;              // 跳出遍历 (1)
                            }
                        }
                        if (!added)
                        {
                            groups1.Add(groups2[idx]);
                        }

                        groups2.RemoveAt(idx);
                        continue;
                    }
                    #endregion

                    #region clear one-item groups
                    // 如果2类分组数量过大，则进行整理
                    if (c == 0 && groups2.Count > reGroupLimit)
                    {
                        // 将只有一个企业的企业分组释放出来
                        for (int k = 0; k < groups2.Count; k++)
                        {
                            reminds.Add(groups2[k].cluster[0]);
                        }
                        groups2.Clear();
                    }
                    else
                    {
                        // 否则，自己成立一个新的分组
                        groups2.Add(new EntPartedGroup(cur));
                    }
                    #endregion
                }
                ents = reminds;
            }

            groups1.AddRange(groups2);
            return groups1;
        }

        private static void SubCluster(IEnumerable<EntPartedFull> ents, List<EntPartedGroup> groups)
        {
            var list = ents.ToList();
            for (int i = 0; i < list.Count; i++)             // 遍历编号：(0)
            {
                var cur = list[i];          // 当前企业
                bool added = false;         // 当前企业是否已经添加到某个分组中

                for (int j = 0; j < groups.Count; j++)      // 遍历编号：(1)
                {
                    var group = groups[j];          // 当前分组
                    int relation = 0;               // 当前分组与当前公司的相关度
                    foreach (var s in cur.total)
                    {
                        if (group.names.Contains(s))
                        {
                            relation++;
                            if (relation == 2) break;
                        }
                    }
                    if (relation == 2)       // 当前分组与当前公司相关，则添加到分组中
                    {
                        group.Add(cur);
                        added = true;
                        break;              // 跳出遍历 (1)
                    }                    
                }
                if(!added)
                {
                    groups.Add(new EntPartedGroup(cur));
                }
            }
        }

        private static void SubCluster(IEnumerable<EntPartedFull> ents, List<EntPartedGroup> groups1, List<EntPartedGroup> groups2)
        {
            var list = ents.ToList();
            for (int i = 0; i < list.Count; i++)             // 遍历编号：(0)
            {
                var cur = list[i];          // 当前企业
                bool added = false;         // 当前企业是否已经添加到某个分组中

                for (int j = 0; j < groups1.Count; j++)      // 遍历编号：(1)
                {
                    var group = groups1[j];          // 当前分组
                    int relation = 0;               // 当前分组与当前公司的相关度
                    foreach (var s in cur.total)
                    {
                        if (group.names.Contains(s))
                        {
                            relation++;
                            if (relation == 2) break;
                        }
                    }
                    if (relation == 2)       // 当前分组与当前公司相关，则添加到分组中
                    {
                        group.Add(cur);
                        added = true;
                        break;              // 跳出遍历 (1)
                    }
                }
                if (added) continue;

                int idx = -1;
                for (int j = 0; j < groups2.Count; j++)      // 遍历编号：(1)
                {
                    var group = groups2[j];          // 当前分组
                    int relation = 0;               // 当前分组与当前公司的相关度
                    foreach (var s in cur.total)
                    {
                        if (group.names.Contains(s))
                        {
                            relation++;
                            if (relation == 2) break;
                        }
                    }
                    if (relation == 2)       // 当前分组与当前公司相关，则添加到分组中
                    {
                        //group.Add(cur);
                        idx = j;
                        break;              // 跳出遍历 (1)
                    }
                }
                if(idx >= 0)
                {
                    groups2[idx].names.UnionWith(cur.total);

                    for(int j = 0; j < groups1.Count; j++)
                    {
                        var relation = 0;
                        foreach(var s in groups2[idx].names)
                        {
                            if(groups1[j].names.Contains(s))
                            {
                                relation++;
                                if (relation == 2) break;
                            }
                        }
                        if(relation == 2)
                        {
                            added = true;
                            groups1[j].Add(cur);
                            groups1[j].Add(groups2[idx].cluster[0]);
                            break;
                        }
                    }
                    if(!added)
                    {
                        groups1.Add(groups2[idx]);
                    }
                    groups2.RemoveAt(idx);
                }
                else
                {
                    groups2.Add(new EntPartedGroup(cur));
                }
            }
        }

        public static List<EntPartedGroup> Cluster_PalOP(List<EntPartedFull> ents)
        {
            var groups1 = new List<EntPartedGroup>();   // 1类分组，包含多个企业
            //var groups2 = new List<EntPartedGroup>();   // 2类分组，包含单个企业

            var groupss1 = new List<List<EntPartedGroup>>();
            var groupss2 = new List<List<EntPartedGroup>>();
            for (int i = 0; i < 8; i++)
            {
                groupss1.Add(new List<EntPartedGroup>());
                groupss2.Add(new List<EntPartedGroup>());
            }
            var list = Partition(ents, 8);
            Parallel.For(0, list.Count, i => SubCluster(list[i], groupss1[i], groupss2[i]));

            ents.Clear();
            for(int i = 0; i < 8; i++)
            {
                //for (int j = groupss2[i].Count - 1; j >= 0; j--)
                //    ents.Add(groupss2[i][j].cluster[0]);
                ents.AddRange(groupss2[i].Select(l => l.cluster[0]));
                groupss2[i].Clear();
            }
            list = Partition(ents, 8);
            Parallel.For(0, list.Count, i => SubCluster(list[i], groupss1[i], groupss2[i]));

            ents.Clear();
            for (int i = 0; i < 8; i++)
            {
                ents.AddRange(groupss2[i].Select(l => l.cluster[0]));
                groupss2[i].Clear();
            }
            list = Partition(ents, 8);
            Parallel.For(0, list.Count, i => SubCluster(list[i], groupss1[i], groupss2[i]));

            for(int i = 0; i < 8; i++)
            {
                groups1.AddRange(groupss1[i]);
                groups1.AddRange(groupss2[i]);
            }

            return groups1;
        }

        public static List<EntPartedGroup> Cluster_Pal(List<EntPartedFull> ents)
        {
            List<EntPartedFull> reminds = null;
            var groups = new List<EntPartedGroup>();
            var groups1 = new List<EntPartedGroup>();

            for (int c = 0; c < 3; c++)
            {
                groups1.Clear();
                reminds = new List<EntPartedFull>();
                var groupss = new List<List<EntPartedGroup>>();
                for(int i = 0; i < 8; i++)
                {
                    groupss.Add(new List<EntPartedGroup>());
                }
                var list = Partition(ents, 8);
                Parallel.For(0, list.Count, i => SubCluster(list[i], groupss[i]));

                for (int i = 0; i < groupss.Count; i++)
                {
                    for (int j = 0; j < groupss[i].Count; j++)
                    {
                        var group = groupss[i][j];
                        bool merge = false;                     // 标识是否合并进入
                        for (int k = 0; k < groups.Count; k++)
                        {
                            int relation = 0;
                            foreach (var s in group.names)
                            {
                                if (groups[k].names.Contains(s))
                                {
                                    relation++;
                                    if (relation == 2) break;
                                }
                            }
                            if (relation == 2)
                            {
                                merge = true;                   // 合并进入，与groups[k]合并
                                foreach (var e in group.cluster)
                                    groups[k].Add(e);

                                break;
                            }
                            // 否则，对下一个分组测试相关度
                        }
                        if (!merge)
                        {
                            if (group.cluster.Count > 1)         //  作为一个新的分组添加进分组列表
                            {
                                groups.Add(group);
                            }
                            else
                            {
                                reminds.Add(group.cluster[0]);      // 添加进剩余企业列表，作为下一轮聚合
                                groups1.Add(group);
                            }
                        }
                    }
                }
                ents = reminds;
            }
            groups.AddRange(groups1);
            return groups;
        }


        /// <summary>
        /// Divides an enumerable into equal parts and perform an action on those parts
        /// </summary>
        /// <remarks><paramref name="enumerable"/>.Count must be larger than 1024</remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable">item set</param>
        /// <param name="parts">how many parts to partition this set</param>
        public static List<IEnumerable<T>> Partition<T>(List<T> enumerable, int parts)
        {
            int count = enumerable.Count;
            int itemsPerPart = count / parts;   // 分区后，每个区的项数量

            var list = new List<IEnumerable<T>>(parts);
            for (int i = 0; i < parts; i++)
            {
                var collection = enumerable.Skip(i * itemsPerPart).Take(i == parts - 1 ? count : itemsPerPart);
                list.Add(collection);
            }
            return list;
        }


        /// <summary>
        /// 为自然人参与的公司聚合排名
        /// </summary>
        /// <param name="list"></param>
        public static void EntPartedSortDescend(List<EntPartedGroup> list)
        {
            for (int i = 1; i < list.Count; i++)
            {
                if (list[i - 1].cluster.Count < list[i].cluster.Count)
                {
                    var temp = list[i];
                    int j = i;
                    while (j > 0 && list[j - 1].cluster.Count < temp.cluster.Count)
                    {
                        list[j] = list[j - 1];
                        j--;
                    }
                    list[j] = temp;
                }
            }
        }

        public static List<RelationNode> SuspiciousRelationNodesDescend(List<RelationNode> nodes)
        {
            var newNodes = new List<RelationNode>();
            var cNodes = new List<RelationNode>();

            for(int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].isCom)
                    cNodes.Add(nodes[i]);
                else
                    newNodes.Add(nodes[i]);
            }

            for(int i = 1; i < cNodes.Count; i++)
            {
                var preNode = cNodes[i - 1];
                var preDegree = preNode.tos.Count + preNode.froms.Count;
                var curNode = cNodes[i];
                var curDegree = curNode.tos.Count + curNode.froms.Count;

                if(preDegree < curDegree)
                {
                    var j = i;
                    var jPreNode = cNodes[j - 1];
                    while(preDegree < curDegree)
                    {
                        cNodes[j] = cNodes[j - 1];
                        j--;
                        if (j <= 0) break;

                        preNode = cNodes[j - 1];
                        preDegree = preNode.tos.Count + preNode.froms.Count;
                    }
                    cNodes[j] = curNode;
                }
            }
            newNodes.AddRange(cNodes);
            return newNodes;
        }
    }
}
