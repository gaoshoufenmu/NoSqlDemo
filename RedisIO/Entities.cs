using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QZ.Redis.Search
{
    /// <summary>
    /// 投资族谱
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class InvCluster
    {
        /// <summary>
        /// 是否是公司节点
        /// </summary>
        public bool isCom;
        /// <summary>
        /// 层级
        /// </summary>
        public int Level;
        /// <summary>
        /// 节点名称
        /// </summary>
        public string name;
        /// <summary>
        /// 分类，投资为1，股东为2，自身为0
        /// </summary>
        public int Category;
        /// <summary>
        /// 节点为公司时，key有效，用于查看公司详情
        /// </summary>
        public string KeyNo;
        /// <summary>
        /// 机构代码，序列化为json时，忽略此字段
        /// </summary>
        [JsonIgnore]
        public string code;
        /// <summary>
        /// 地区代码
        /// </summary>
        [JsonIgnore]
        public string area;

        /// <summary>
        /// 分类下节点数量
        /// </summary>
        public int total;
        /// <summary>
        /// 描述，比如是股东，则为投资比例，如果是成员，则为职位名
        /// </summary>
        public string des;

        public List<InvCluster> children;

        public InvCluster(string name, string code, string area)
        {
            this.name = name;
            this.code = code;
            this.area = area;
            KeyNo = Util.ToHexStr(code + "|" + area);
        }

        public InvCluster(string name, int level, int cat)
        {
            this.Category = cat;
            this.name = name;
            this.Level = level;
        }
        public void SetCodeArea(string codeArea)
        {
            var segs = codeArea.Split('|');
            if (segs.Length == 2)
                area = segs[1];
            code = segs[0];
            isCom = true;
            KeyNo = Util.ToHexStr(codeArea);
        }

        public static InvCluster Fake(string name, string code, string area)
        {
            var map = new InvCluster(name.NormalizeComName(), code, area);
            map.isCom = true;
            map.children = new List<InvCluster>()
            {
                new InvCluster(name, 0, 1) { code = code },           // 投资
                new InvCluster(name, 0, 2) { code = code }            // 股东
            };
            return map;
        }

    }

    /// <summary>
    /// 新股权结构
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class Relations
    {
        public List<RelationNode> Nodes;
        public List<RelationLink> Links;

        public Relations()
        {
            Nodes = new List<RelationNode>();
            Links = new List<RelationLink>();
        }

        public static Relations Fake(string name, string code, string area)
        {
            var structs = new Relations();
            structs.Nodes = new List<RelationNode>() { new RelationNode(name.NormalizeComName(), code, area) };
            structs.Links = new List<RelationLink>();
            return structs;
        }
    }
    [JsonObject(MemberSerialization.OptOut)]
    public class RelationNode
    {
        /// <summary>
        /// 节点唯一id
        /// </summary>
        public int id;
        /// <summary>
        /// 节点为公司时，其机构代码
        /// </summary>
        [JsonIgnore]
        public string code;
        /// <summary>
        /// 地区代码
        /// </summary>
        [JsonIgnore]
        public string area;
        ///// <summary>
        ///// 出入度
        ///// </summary>
        //public int inout;
        /// <summary>
        /// 层级，自身节点为0
        /// </summary>
        public int Level;
        public string KeyNo;

        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name;
        /// <summary>
        /// 是否是公司节点
        /// </summary>
        public bool isCom;

        public int Category = 2;
        /// <summary>
        /// 去向
        /// </summary>
        [JsonIgnore]
        public Dictionary<int, List<string>> tos = new Dictionary<int, List<string>>();
        /// <summary>
        /// 来向
        /// </summary>
        [JsonIgnore]
        public HashSet<int> froms = new HashSet<int>();

        /// <summary>
        /// 公司节点
        /// </summary>
        /// <param name="name"></param>
        /// <param name="code"></param>
        /// <param name="area"></param>
        public RelationNode(string name, string code, string area)
        {
            isCom = true;
            Category = 1;
            this.Name = name;
            this.code = code;
            this.area = area;
            KeyNo = Util.ToHexStr(code + "|" + area);
        }
        public RelationNode(string name, int id)
        {
            this.Name = name;
            this.id = id;
        }
        public RelationNode(string name, int level, int id)
        {
            this.Name = name;
            this.Level = level;
            this.id = id;
        }

        public void SetCodeArea(string codeArea)
        {
            var segs = codeArea.Split('|');
            if (segs.Length == 2)
                area = segs[1];
            code = segs[0];
            isCom = true;
            Category = 1;
            KeyNo = Util.ToHexStr(codeArea);
        }

        public void AddTarget(int id, string edge)
        {
            List<string> list;
            if (tos.TryGetValue(id, out list))
            {
                list.Add(edge);
            }
            else
            {
                tos[id] = new List<string>() { edge };
            }
        }
    }

    /// <summary>
    /// 关系
    /// </summary>
    public class RelationLink
    {
        /// <summary>
        /// 关系起点
        /// </summary>
        public int start;
        /// <summary>
        /// 关系终点
        /// </summary>
        public int end;
        public string Relation;

        public RelationLink(int start, int end, string value)
        {
            this.start = start;
            this.end = end;
            this.Relation = value;
        }
    }

    /// <summary>
    /// 股权结构
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class StockStruct
    {
        /// <summary>
        /// 节点为公司时，其机构代码
        /// </summary>
        [JsonIgnore]
        public string code;
        /// <summary>
        /// 地区代码
        /// </summary>
        [JsonIgnore]
        public string area;
        /// <summary>
        /// 层级，自身节点为0
        /// </summary>
        public int level;
        public string key;

        /// <summary>
        /// 节点名称
        /// </summary>
        public string name;
        /// <summary>
        /// 与父节点连接的边，股权比例
        /// 股权double值转为字符串然后append一个'%'
        /// </summary>
        public string edge;
        /// <summary>
        /// 股东节点
        /// </summary>
        public List<StockStruct> children;
        /// <summary>
        /// 是否是公司节点
        /// </summary>
        public bool isCom;
        public StockStruct(string name, string edge, int level)
        {
            this.name = name;
            this.level = level;
            this.edge = edge;
        }



        public StockStruct(string name, string code, string area)
        {
            this.name = name;
            this.code = code;
            this.area = area;
            key = Util.ToHexStr(code + "|" + area);
        }

        public void SetCodeArea(string codeArea)
        {
            var segs = codeArea.Split('|');
            if (segs.Length == 2)
                area = segs[1];
            code = segs[0];
            isCom = true;
            key = Util.ToHexStr(codeArea);
        }

        public static StockStruct Fake(string name, string code, string area)
        {
            var ss = new StockStruct(name.NormalizeComName(), code, area);
            ss.isCom = true;
            return ss;
        }
    }

    /// <summary>
    /// 实际控制人
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class ActualController
    {
        /// <summary>
        /// 节点名
        /// </summary>
        public string name;
        /// <summary>
        /// 边字符串，权值比例
        /// </summary>
        public string edge;
        /// <summary>
        /// 如果是公司节点，存储code和area信息
        /// </summary>
        public string key;
        /// <summary>
        /// 机构代码
        /// </summary>
        [JsonIgnore]
        public string code;
        /// <summary>
        /// 地区码
        /// </summary>
        [JsonIgnore]
        public string area;
        /// <summary>
        /// 是否是公司节点
        /// </summary>
        public bool isCom;
        /// <summary>
        /// 层级，自身节点为0
        /// </summary>
        public int level;
        /// <summary>
        /// 子节点
        /// </summary>
        public ActualController child;

        public ActualController(string name, string code, string area)
        {
            this.name = name;
            this.code = code;
            this.area = area;
            this.key = Util.ToHexStr(code + "|" + area);
        }
        public ActualController(string name, string edge, int level)
        {
            this.name = name;
            this.edge = edge;
            this.level = level;
        }

        public void SetCodeArea(string codeArea)
        {
            var segs = codeArea.Split('|');
            if (segs.Length == 2)
                area = segs[1];
            code = segs[0];
            isCom = true;
            key = Util.ToHexStr(codeArea);
        }
        public static ActualController Fake(string name, string code, string area)
        {
            var ac = new ActualController(name.NormalizeComName(), code, area);
            ac.isCom = true;
            return ac;
        }
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class StockController
    {
        /// <summary>
        /// 股权结构图中所有节点数量，如果所有节点数量为1，且为自然人节点，则当投资比例为0时，修改为100%
        /// 不包含self节点
        /// </summary>
        public static int allNodesCount;
        /// <summary>
        /// 节点唯一编号
        /// </summary>
        public int id;
        /// <summary>
        /// 节点为公司时，其机构代码
        /// </summary>
        [JsonIgnore]
        public string code;
        /// <summary>
        /// 地区代码
        /// </summary>
        [JsonIgnore]
        public string area;
        /// <summary>
        /// 层级，自身节点为0
        /// </summary>
        public int Grade = 1;
        /// <summary>
        /// 公司节点唯一性编码，用于查询公司详情
        /// </summary>
        public string KeyNo;

        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name;
        /// <summary>
        /// 与父节点连接的边，股权比例
        /// 股权double值转为字符串然后append一个'%'
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string FundedRatio;
        /// <summary>
        /// 股权比例（double类型）
        /// </summary>
        [JsonIgnore]
        public double ratio;
        /// <summary>
        /// 节点权重
        /// </summary>
        [JsonIgnore]
        public double weight;
        /// <summary>
        /// 权重是否是最大的
        /// </summary>
        [JsonIgnore]
        public bool isMax;
        /// <summary>
        /// 节点
        /// </summary>
        [JsonIgnore]
        public StockController parent;
        /// <summary>
        /// 股东节点，股权结构图中
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<StockController> Children;
        ///// <summary>
        ///// 是否是公司节点
        ///// </summary>
        //public bool isCom;
        /// <summary>
        /// 1为公司， 2为自然人
        /// </summary>
        public int Category = 2;
        /// <summary>
        /// 实际控制人路径（最优路径）上的节点集合
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ControlNode> ActualControllorPath;
        /// <summary>
        /// 实际控制人图中所有节点（包括self节点）
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ControlNode> Nodes;
        /// <summary>
        /// 实际控制人图中的所有连接边
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ControlLink> Links;
        /// <summary>
        /// 实际控制人，用于文本显示
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ActualController;
        /// <summary>
        /// 实际控制人的总比例
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TotalRatio;

        public StockController(string name, string code, string area)
        {
            this.Name = name;
            this.code = code;
            this.area = area;
            Category = 1;
            KeyNo = Util.ToHexStr(code + "|" + area);
        }

        public StockController(string name, int grade, double ratio, int id)
        {
            this.id = id;
            this.Name = name;
            this.Grade = grade;
            this.ratio = ratio;
            this.KeyNo = Util.ToHexStr(name);
            FundedRatio = ratio.ToString() + "%";
        }
        public void SetCodeArea(string codeArea)
        {
            var segs = codeArea.Split('|');
            if (segs.Length == 2)
                area = segs[1];
            code = segs[0];
            Category = 1;
            KeyNo = Util.ToHexStr(codeArea);
        }

        public static StockController Fake(string name, string code, string area)
        {
            var sc = new StockController(name.NormalizeComName(), code, area);
            sc.weight = 1;                                                              // 自身节点权重为1
            sc.Children = new List<StockController>();
            sc.ActualControllorPath = new List<ControlNode>();
            //sc.Links = new List<ControlLink>();
            sc.Nodes = new List<ControlNode>();
            return sc;
        }
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class ControlNode
    {
        /// <summary>
        /// 层级，自身节点为0
        /// </summary>
        public int Grade;
        /// <summary>
        /// 公司节点唯一性编码，用于查询公司详情
        /// 自然人节点目前没有赋值，感觉也不需要赋值
        /// </summary>
        public string KeyNo;
        /// <summary>
        /// 1为公司， 2为自然人
        /// </summary>
        public int Category;
        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name;
        /// <summary>
        /// 与父节点连接的边，股权比例
        /// 股权double值转为字符串然后append一个'%'
        /// </summary>
        public string FundedRatio;
        /// <summary>
        /// 是否是最优路径
        /// </summary>
        [JsonIgnore]
        public bool isOptimized;

        public ControlNode(string name, string key, int cat, int grade, string ratio)
        {
            this.Name = name;
            this.KeyNo = key;
            Category = cat;
            Grade = grade;
            FundedRatio = ratio;
        }
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class ControlLink
    {
        public string start;
        public string end;
        public string Ratio;
        public bool isController;

        public ControlLink(string start, string end, string ratio)
        {
            this.start = start;
            this.end = end;
            this.Ratio = ratio;
        }
    }

    /// <summary>
    /// 企业图谱
    /// 也可以使用<seealso cref="InvCluster"/>，其实本质上不需要多次递归的类型，故这里重新定义一个类
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class EntMap
    {
        public string name;
        [JsonIgnore]
        public string code;
        [JsonIgnore]
        public string area;
        public string key;
        public int level;
        public bool isCom;
        /// <summary>
        /// 分类，自身为0
        /// 对外投资1
        /// 股东2
        /// 高管3
        /// 文书4
        /// 公告5327547462
        /// </summary>
        public int cat;

        /// <summary>
        /// 分类下节点数量
        /// </summary>
        public int total;
        /// <summary>
        /// 描述，比如是股东，则为投资比例，如果是成员，则为职位名
        /// </summary>
        public string des;

        public List<EntMap> children;


        public EntMap(string name, string code, string area)
        {
            this.name = name;
            this.code = code;
            this.area = area;
            key = Util.ToHexStr(code + "|" + area);
        }
        public EntMap(string name, int cat, int level)
        {
            this.name = name;
            this.cat = cat;
            this.level = level;
        }

        public EntMap(int cat, int level, string code)
        {
            this.cat = cat;
            this.level = level;
            this.code = code;
        }

        public void SetCodeArea(string codeArea)
        {
            var segs = codeArea.Split('|');
            if (segs.Length == 2)
                area = segs[1];
            code = segs[0];
            isCom = true;
            key = Util.ToHexStr(codeArea);
        }

        public void CalcKeyNo()
        {
            isCom = true;
            key = Util.ToHexStr(code + "|" + area);
        }

        public static EntMap Fake(string name, string code, string area)
        {
            var map = new EntMap(name.NormalizeComName(), code, area);
            map.isCom = true;
            map.children = new List<EntMap>()
            {
                new EntMap("对外投资", 1, 0) { children = new List<EntMap>() },
                new EntMap("股东", 2, 0) { children = new List<EntMap>() },
                new EntMap("高管", 3, 0) { children = new List<EntMap>() },
                new EntMap("文书", 4, 0) { children = new List<EntMap>() },
                new EntMap("公告", 5, 0) { children = new List<EntMap>() }
            };
            return map;
        }
    }
    /// <summary>
    /// 单个公司信息
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class EntPartedFull
    {
        /// <summary>
        /// legal person
        /// </summary>
        public string lp;

        /// <summary>
        /// 公司名
        /// </summary>
        public string name;
        /// <summary>
        /// 地区代码
        /// </summary>
        public string area;
        /// <summary>
        /// 机构代码
        /// </summary>
        public string code;

        [JsonIgnore]
        public bool isSelf;

        [JsonIgnore]
        public List<string> shares;
        [JsonIgnore]
        public List<string> members;
        [JsonIgnore]
        public List<string> invs;
        /// <summary>
        /// 合并
        /// </summary>
        [JsonIgnore]
        public HashSet<string> total;

        /// <summary>
        /// 自然人列表
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, StrIntPair> pnameDict = new Dictionary<string, StrIntPair>();

        public EntPartedFull(string code, string area, string name)
        {
            this.name = name;
            this.code = code;
            this.area = area;

            total = new HashSet<string>() { name };
        }

        public EntPartedFull()
        {
            total = new HashSet<string>() { name };
        }

        public EntPartedFull(string name)
        {
            this.name = name;
            total = new HashSet<string>() { name };
        }
    }

    public class EntPartedGroups
    {
        public int TotalCount { get; set; }

        public List<EntPartedGroup> EntPartedGroupList { get; set; }
    }

    /// <summary>
    /// 公司组
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class EntPartedGroup
    {
        /// <summary>
        /// 分组总数量
        /// </summary>
        public static int total;
        /// <summary>
        /// 分页，每个页面大小
        /// </summary>
        [JsonIgnore]
        public static int pgSize;

        [JsonIgnore]
        public bool isSelf;
        /// <summary>
        /// 组内公司列表
        /// </summary>
        public List<EntPartedFull> cluster;
        /// <summary>
        /// 自然人名与出现频率映射
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, StrIntPair> pnameDict = new Dictionary<string, StrIntPair>();
        /// <summary>
        /// 可能认识的人名
        /// 人名: 机构代码 map
        /// </summary>
        public List<Tuple<string, string>> knowns;
        /// <summary>
        /// 当前关联的自然人名
        /// </summary>
        public string pname;
        /// <summary>
        /// 所有名称，包括成员，股东，对外投资
        /// </summary>
        public HashSet<string> names = new HashSet<string>();

        public EntPartedGroup()
        {
            cluster = new List<EntPartedFull>();
        }

        public void Add(EntPartedFull ep)
        {
            cluster.Add(ep);
            names.UnionWith(ep.total);
            foreach (var p in ep.pnameDict)
            {
                if (pnameDict.ContainsKey(p.Key))
                    pnameDict[p.Key].@int += p.Value.@int;
                else
                    pnameDict[p.Key] = p.Value;
            }

            if (ep.isSelf)
                isSelf = true;
        }

        public void Extend(EntPartedGroup grp)
        {
            cluster.AddRange(grp.cluster);
            names.UnionWith(grp.names);

            foreach (var p in grp.pnameDict)
            {
                if (pnameDict.ContainsKey(p.Key))
                    pnameDict[p.Key].@int += p.Value.@int;
                else
                    pnameDict[p.Key] = p.Value;
            }

            if (grp.isSelf)
                isSelf = true;
        }


        public EntPartedGroup(EntPartedFull ep)
        {
            cluster = new List<EntPartedFull>() { ep };
            names.UnionWith(ep.total);
            foreach(var p in ep.pnameDict)
            {
                if (pnameDict.ContainsKey(p.Key))
                    pnameDict[p.Key].@int += p.Value.@int;
                else
                    pnameDict[p.Key] = p.Value;
            }

            if (ep.isSelf)
                isSelf = true;
        }

        public int GetRelation(EntPartedFull ep) => ep.total.Intersect(names).Count();
        public bool IsRelated(EntPartedFull ep)
        {

            int count = 0;
            foreach(var s in ep.total)
            {
                if(names.Contains(s))
                {
                    count++;
                    if (count == 2)
                        return true;
                }
            }
            return false;
        }

        public void SetKnowns(string pname)
        {
            this.pname = pname;
            // 认识的人数上限设置为5
            //! 可以调整人数上限
            var total = pnameDict.ToList();
            var list = new List<KeyValuePair<string, StrIntPair>>(total.Take(5).OrderByDescending(p => p.Value.@int));
            for(int i = 5; i < total.Count; i++)
            {
                var t = total[i];
                for(int j = 0; j < 5; j++)
                {
                    var l = list[j];        // 当前某个认识的人，位置为 j，
                    if(t.Value.@int > l.Value.@int)   // 如果认识度小于后面的人，则需要将后面的人插在此位置 j
                    {
                        // j+1 位置上值改为j位置上值，j位置上值改为t
                        for(int k = 4; k > j; k--)
                        {
                            list[k] = list[k - 1];
                        }
                        //
                        list[j] = t;
                        break;
                    }
                }
            }
            knowns = list.Select(p => new Tuple<string, string>(p.Key, p.Value.str)).ToList();
        }
    }

    public class EntParted
    {
        public string name;
        public string area;
        public string code;
        public bool isSelf;
        public int score;

        public EntParted(string code, string area, string name)
        {
            this.name = name;
            this.code = code;
            this.area = area;
        }

        public EntParted(string name)
        {
            this.name = name;
        }
    }


    public class StrIntPair
    {
        public string str;
        public int @int;

        public StrIntPair(string s, int i)
        {
            str = s;
            @int = i;
        }
    }

    /// <summary>
    /// 找关系类
    /// </summary>
    public class Relation
    {
        /// <summary>
        /// 顶点集合
        /// </summary>
        public List<RelationVertex> vertices;
        /// <summary>
        /// 边集合
        /// </summary>
        public List<RelationEdge> edges;

        /// <summary>
        /// 路径
        /// </summary>
        public List<RelationPath> paths;

        public Relation()
        {
            //vertices = new List<RelationVertex>();
            edges = new List<RelationEdge>();
            paths = new List<RelationPath>();
        }
    }

    /// <summary>
    /// 找关系图中的关系顶点
    /// </summary>
    public class RelationVertex
    {
        /// <summary>
        /// 0, start vertex
        /// 1, end vertex
        /// 2, middle vertex
        /// </summary>
        public int part;
        /// <summary>
        /// 层级，起点为1，以后依次递增1
        /// </summary>
        public int lvl;
        /// <summary>
        /// 是否是公司顶点
        /// </summary>
        public bool isCom;
        /// <summary>
        /// 顶点唯一键
        /// 用于查询公司详情
        /// </summary>
        public string keyNo;
        /// <summary>
        /// 顶点在关系图中的id
        /// </summary>
        public int id;
        /// <summary>
        /// 顶点名称
        /// </summary>
        public string name;
        /// <summary>
        /// 关联的边id集合，包括入向边和出向边
        /// </summary>
        public HashSet<int> edgeIds;

        public RelationVertex(int id, int part, string name)
        {
            this.id = id;
            this.part = part;
            this.name = name;
        }

        public RelationVertex(string name, int id, int lvl)
        {
            this.id = id;
            this.lvl = lvl;
            this.name = name;
        }

        public void SetCodeArea(string code, string area)
        {
            keyNo = Util.ToHexStr(code + "|" + area);
            isCom = true;
        }
    }
    /// <summary>
    /// 找关系图中的边
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class RelationEdge
    {
        /// <summary>
        /// 关系图中边的唯一id
        /// 与顶点分属两个domain，故，与顶点id具有相同值域
        /// </summary>
        public int id;
        /// <summary>
        /// 边的起始顶点id
        /// </summary>
        public int endId;
        /// <summary>
        /// 边的结束顶点id
        /// </summary>
        public int startId;
        [JsonIgnore]
        public string startName;
        [JsonIgnore]
        public string endName;

        /// <summary>
        /// 边的文本
        /// </summary>
        public string text;

        public RelationEdge(int id, int startId, int endId, string text)
        {
            this.id = id;
            this.endId = startId;
            this.startId = endId;
            this.text = text;
        }
    }
    /// <summary>
    /// 找关系图中的路径
    /// </summary>
    public class RelationPath
    {
        /// <summary>
        /// 路径中点（站）数
        /// </summary>
        public int count;
        /// <summary>
        /// 路径id
        /// </summary>
        public int id;
        /// <summary>
        /// 顺序是否相反？
        /// 时间久远，记不清了...
        /// </summary>
        public List<PathPoint> parts;

        public RelationPath(int id)
        {
            this.id = id;
            parts = new List<PathPoint>();
        }

        public void AddPoint(PathPoint point)
        {
            parts.Add(point);
        }
    }

    /// <summary>
    /// 找关系图中的路径点
    /// </summary>
    public class PathPoint
    {
        public bool isCom;
        /// <summary>
        /// 路径点名称
        /// </summary>
        public string name;
        /// <summary>
        /// 与上一个路径点之间的关系
        /// </summary>
        public string text;
        /// <summary>
        /// 如果是公司，则表示唯一键，用于查询详情
        /// </summary>
        public string keyNo;
        /// <summary>
        /// 所属路径id
        /// </summary>
        public int pathId;

        public PathPoint(string name, string text, int pathId)
        {
            this.name = name;
            this.text = text;
            this.pathId = pathId;
        }
    }

    /// <summary>
    /// 找关系辅助类
    /// </summary>
    public class RelationChunk:IEquatable<RelationChunk>
    {
        /// <summary>
        /// 0, start vertex
        /// 1, end vertex
        /// 2, middle vertex
        /// </summary>
        public int part;

        public bool isCom;
        /// <summary>
        /// 层级，起点为1，以后依次递增1
        /// </summary>
        public int lvl;

        public string name;
        public string code;
        public string area;

        public bool isEnd;
        /// <summary>
        /// 与父节点的边文本
        /// </summary>
        public string edge;
        /// <summary>
        /// whether the edge is child pointing to parent
        /// </summary>
        public bool c2p;
        /// <summary>
        /// 父节点
        /// </summary>
        public RelationChunk parent;

        private int _pathHash = 0;
        /// <summary>
        /// 
        /// </summary>
        public int PathHash
        {
            get
            {
                if (_pathHash == 0)
                {
                    var list = new List<string>();
                    int c = 0;
                    var cur = this;
                    // calc path hash value, but it is directive related
                    while (cur != null)
                    {
                        if (c > 1)
                        {
                            list.Add(cur.name);
                        }
                        c++;
                        cur = cur.parent;
                        if (cur != null && cur.parent != null && cur.parent.parent == null) break;
                    }
                    int mask = int.MinValue;

                    for(int i = 0; i < list.Count; i++)
                    {
                        _pathHash = (_pathHash << 5 & mask) | (_pathHash >> 27);
                        _pathHash += list[i].GetHashCode();
                    }
                    //var stack = new Stack<RelationChunk>();
                    
                    
                    //// calc path hash value, but it is directive related
                    //while (cur != null)
                    //{
                    //    stack.Push(cur);
                    //    _pathHash = (_pathHash << 3 & mask) | (_pathHash >> 29);
                    //    _pathHash += cur.name.GetHashCode();
                    //    cur = cur.parent;
                    //}
                    
                    //var hash2 = 0;
                    //// calc the reverse direction's path hash
                    //while(stack.Count > 0)
                    //{
                    //    var item = stack.Pop();
                    //    hash2 = (hash2 << 3 & mask) | (hash2 >> 29);
                    //    hash2 += item.name.GetHashCode();
                    //}
                    //// get the sum which eliminates the directive relation
                    //_pathHash += hash2;

                    // however, we can use another way to get the same purpose if we can sure that there is no overlapped edges
                    // just for impove a little performance
                    // later to implement this algorithm
                }
                return _pathHash;
            }
        }


        private string _rootName;
        public string RootName
        {
            get
            {
                if(_rootName == null)
                {
                    var next = this;
                    var pre = parent;
                    while (pre != null)
                    {
                        next = pre;
                        pre = pre.parent;
                    }
                    _rootName = next.name;
                }
                return _rootName;
            }
        }
        

        /// <summary>
        /// 子节点
        /// </summary>
        public List<RelationChunk> children;

        public RelationChunk(string name)
        {
            this.name = name;
        }
        public RelationChunk(string name, string code, string area)
        {
            this.name = name;
            this.code = code;
            this.area = area;
        }
        public void SetCodeArea(string codeArea)
        {
            var segs = codeArea.Split('|');
            if (segs.Length == 2)
                area = segs[1];
            code = segs[0];
            isCom = true;
            //KeyNo = Util.ToHexStr(codeArea);
        }

        public RelationChunk Clone()
        {
            var chk = new RelationChunk(name, code, area);
            chk.isEnd = isEnd;
            chk.isCom = isCom;
            chk.edge = edge;
            chk.c2p = c2p;
            if (part == 2)
                chk.part = 2;
            else if (part == 0)
                chk.part = 1;

            //? todo: 设置edge
            return chk;
        }

        public bool Equals(RelationChunk other) => name == other.name && code == other.code;

        public static RelationChunk MemberwiseClone(RelationChunk chunk)
        {
            var chk = new RelationChunk(chunk.name, chunk.code, chunk.area);
            chk.parent = chunk.parent;
            chk.isCom = chunk.isCom;
            chk.edge = chunk.edge;
            chk.c2p = chunk.c2p;
            chk.lvl = chunk.lvl;
            return chk;
        }
    }

    /// <summary>
    /// 针对每一次找关系时，生成对应的临时类，用于保存一些信息
    /// </summary>
    public class RelationTmp
    {
        /// <summary>
        /// 所有节点名到唯一id的映射
        /// </summary>
        public Dictionary<string, int> name2Id;
        ///// <summary>
        ///// 所有节点名列表
        ///// </summary>
        //public List<string> names;
        public BigList<RelationEntity> entities;
        /// <summary>
        /// 实体间的边字典
        /// key为实体start和end的id: startId &lt;&lt; 32 | endId
        /// value为边文本
        /// </summary>
        public Dictionary<long, RelationSide> edgeDict;

        /// <summary>
        /// 是否是短路径，不超过4认为是短路径
        /// </summary>
        public bool isShortDistance;
        /// <summary>
        /// 原始输入实体列表
        /// </summary>
        public List<RelationEntity> entities_r;

        public List<string> prefixs;

        /// <summary>
        /// 原始公司与原始自然人之间的关系，order与原始顺序保持一致
        /// </summary>
        public RelationSide[] edges_r;

        /// <summary>
        /// 构造器
        /// </summary>
        /// <param name="cnames">公司名列表，不可为null</param>
        /// <param name="pnames">自然人名列表，不可为null</param>
        /// <param name="distance">距离</param>
        public RelationTmp(List<string> cnames, List<string> pnames, int distance)
        {
            // 估测所有可能的实体，包括中间或临时实体，是输入实体数量的指数
            var count = cnames.Count + pnames.Count;    // -_-!!, 我相信 > 0，--
            entities_r = new List<RelationEntity>(count);

            if (distance > 4)
                distance = 4;
            count *= (int)Math.Pow(15, distance);

            name2Id = new Dictionary<string, int>(count);
            //names = new List<string>(count);
            entities = new BigList<RelationEntity>();
            edgeDict = new Dictionary<long, RelationSide>(count);
        }

        public RelationTmp(int distance)
        {
            if (distance > 6)
                distance = 6;
            var count = (int)Math.Pow(5, distance);
            name2Id = new Dictionary<string, int>(count);
            entities = new BigList<RelationEntity>();
            edgeDict = new Dictionary<long, RelationSide>(count);
        }

        /// <summary>
        /// 注册一个实体名
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public RelationEntity RegisterEntity(string name, bool isCom, bool isRaw = false)
        {
            int id;
            if (name2Id.TryGetValue(name, out id))
                return entities[id];

            id = entities.Count;
            var e = new RelationEntity(name) { isCom = isCom, id = id, isRaw = isRaw };
            entities.Add(e);
            if (isRaw)
            {
                entities_r.Add(e);
            }
            //names.Add(name);
            name2Id[name] = id;

            return e;
        }



        /// <summary>
        /// 获取实体名
        /// </summary>
        /// <param name="id">实体id，必须非负</param>
        /// <returns></returns>
        public string GetName(int id)
        {
            if (id < entities.Count)
                return entities[id].name;
            return null;
        }

        /// <summary>
        /// 添加一个边
        /// </summary>
        /// <param name="startId">起始实体id</param>
        /// <param name="endId">结束实体id</param>
        /// <param name="edge">边文本，不可为null</param>
        /// <returns></returns>
        public long RegisterEdge(int startId, int endId, string edge)
        {
            var lid = ((long)startId << 32) | endId;
            var rlid = ((long)endId << 32) | startId;
            RelationSide ew;
            if (edgeDict.TryGetValue(lid, out ew) || edgeDict.TryGetValue(rlid, out ew))
            {
                ew.AddEdges(edge.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                ew = new RelationSide(edgeDict.Count, startId, endId);
                ew.AddEdges(edge.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                edgeDict.Add(lid, ew);
            }
            return lid;
        }

        public void AddOriginEdge(int startId, int endId, string edge, int id)
        {
            var ew = edges_r[id];
            if(ew != null)
            {
                ew.AddEdges(edge.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
            else
            {
                ew = new RelationSide(-id, startId, endId);
                ew.AddEdges(edge.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                edges_r[id] = ew;
            }
        }

        public RelationSide GetEdge(int startId, int endId)
        {
            var lid = ((long)startId << 32) | endId;
            var rlid = ((long)endId << 32) | startId;
            RelationSide ew;
            if (edgeDict.TryGetValue(lid, out ew) || edgeDict.TryGetValue(rlid, out ew))
            {
                return ew;
            }
            return null;
        }

        public void Clear()
        {
            edgeDict.Clear();
            name2Id.Clear();
            entities.Clear();
        }
    }

    /// <summary>
    /// startId and endId may reverse the direction
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class RelationSide
    {
        [JsonIgnore]
        public HashSet<string> edges = new HashSet<string>();
        [JsonIgnore]
        public bool hasSenior;
        [JsonIgnore]
        public bool hasPosition;

        public int id;
        /// <summary>
        /// 边的起始顶点id
        /// </summary>
        public int startId;
        /// <summary>
        /// 边的结束顶点id
        /// </summary>
        public int endId;
        public RelationSide(int id, int startId, int endId)
        {
            this.id = id;
            this.startId = startId;
            this.endId = endId;
        }

        //public long lid;
        
        public void AddEdges(string[] es)
        {
            for(int i = 0; i < es.Length; i++)
            {
                var e = es[i];

                AddEdge(e);
            }
        }
        public void AddEdge(string e)
        {
            if (e.Length == 2 && e[0] == '高' && e[1] == '管')
            {
                // 遇到高管
                if (hasPosition)
                {
                    // 如果已经有了职位，则忽略高管这个文本
                    return;
                }
                else
                {
                    // 如果还没有职位，则添加高管
                    edges.Add(e);
                    hasSenior = true;
                }
            }
            else if (e.Length == 2 && e[0] == '股' && e[1] == '东')
            {
                // 遇到股东，直接添加
                edges.Add(e);
            }
            else
            {
                // 遇到职位
                if (hasSenior)
                {
                    // 如果有有高管，则去掉高管
                    hasSenior = false;
                    hasPosition = true;

                    edges.Remove("高管");
                }
                edges.Add(e);
            }
        }

        private string _text;
        public string text
        {
            get
            {
                if (_text == null)
                {
                    var sb = new StringBuilder(edges.Count * 2);
                    foreach (var s in edges)
                    {
                        if (sb.Length > 0)
                            sb.Append(',');
                        sb.Append(s);
                    }
                    _text = sb.ToString();
                }
                return _text;
            }
        }
    }

    public class RelationShip
    {
        public List<RelationEntity> vertices;
        public List<RelationSide> edges;

        public static RelationShip Default() => new RelationShip() { vertices = new List<RelationEntity>(), edges = new List<RelationSide>() };
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptOut)]
    public class RelationEntity
    {
        public int id;
        /// <summary>
        /// 是否是原始输入节点
        /// </summary>
        public bool isRaw;
        public string name;
        [JsonIgnore]
        public string code;
        [JsonIgnore]
        public string area;

        public bool isCom;
        public int lvl;
        public string keyNo;

        /// <summary>
        /// 相邻节点id集合
        /// 如果性能不行，那只能自己撸了
        /// </summary>
        [JsonIgnore]
        public SortedSet<int> ajacentIds = new SortedSet<int>();

        public RelationEntity(string name, string code, string area)
        {
            this.name = name;
            this.code = code;
            this.area = area;
            isCom = true;
            keyNo = Util.ToHexStr(code + "|" + area);
        }

        public RelationEntity(string name)
        {
            this.name = name;
        }

        public void SetCodeArea(string codeArea)
        {
            var segs = codeArea.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if(segs.Length == 2)
            {
                code = segs[0];
                area = segs[1];
                keyNo = Util.ToHexStr(code + "|" + area);
            }
        }
    }

    public class RelationTree
    {
        public RelationEntity root;

    }

    public class MultiTrueRelationInternal
    {
        public Dictionary<string, RelationEntity> newCDict = new Dictionary<string, RelationEntity>();
        public Dictionary<string, RelationEntity> newPDict = new Dictionary<string, RelationEntity>();
    }
}
