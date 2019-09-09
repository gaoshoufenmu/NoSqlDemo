using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticsearchIO
{
    public class ESWrapper<T>
    {
        public long took { get; set; }
        public long total { get; set; }

        public List<DocWrapper<T>> docs { get; set; }

        public List<Agg> aggs { get; set; }
    }

    public class DocWrapper<T>
    {
        public T doc { get; set; }

        public Dictionary<string, string> hl { get; set; }
        public double score { get; set; }
    }

    public class Agg
    {
        public string name;
        public List<AggItem> items;
    }

    public class AggItem
    {
        public string name;
        public long count;
    }


    public class FamilyNamePack
    {
        /// <summary>
        /// 各地区数量统计
        /// </summary>
        public Dictionary<string, int> Area2Num;

        /** 根据页码和页大小，返回当前页的数据 **/

        /**
         * 当列表中只有一个element，一般是因为搜索时指定了姓氏
         * 也可能是指定了地区筛选条件，且那个地区下仅有一个姓氏
         * 当然，调用者应该清楚
         * */
        /// <summary>
        /// 姓氏列表
        /// </summary>
        public List<FamilyName_List> list = new List<FamilyName_List>();


        /// <summary>
        /// 总页数，分页展示时用到
        /// </summary>
        public int totalpage;
    }

    /// <summary>
    /// 单个姓氏
    /// </summary>
    public class FamilyName_List
    {
        /// <summary>
        /// 姓氏
        /// </summary>
        public string Familyname;
        /// <summary>
        /// 姓氏关联的自然人列表
        /// </summary>
        public List<FN_Name_List> list;
        public int total;
    }

    /// <summary>
    /// 单个姓名
    /// </summary>
    public class FN_Name_List
    {
        /// <summary>
        /// 自然人姓名
        /// </summary>
        public string name;
        /// <summary>
        /// 自然人关联公司
        /// </summary>
        public List<ComMini> list;
        public int total;
    }

    public class ComMini
    {
        public string name;
        public string code;
        public string area;
    }
}
