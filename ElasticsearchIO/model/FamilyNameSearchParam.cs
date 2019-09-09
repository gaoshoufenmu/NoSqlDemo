using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticsearchIO
{
    public class FamilyNameSearchParam
    {
        public string ESIndex = "familyname_v0";
        public string ESType = "Familyname";

        public enum PersonType
        {
            member
        }

        #region 设置筛选条件
        /// <summary>
        /// 设置地区码
        /// 如设置此字段，仅在长度为2或4时有效
        /// </summary>
        public string areacode;
        /// <summary>
        /// 类型
        /// </summary>
        public PersonType ptype = PersonType.member;
        /// <summary>
        /// 分页展示，页码（从0开始）
        /// </summary>
        public int pg_index = 0;
        /// <summary>
        /// 页大小，越大速度越慢，尽量不要超过10
        /// </summary>
        public int pg_size = 10;
        /// <summary>
        /// 指定姓氏
        /// </summary>
        public string Familyname;
        /// <summary>
        /// 指定姓名
        /// 使用基于图谱数据的接口实现，详情refer to `QZ.OrgCompanyNewMapService.MapReader.GetComGroupsByPerson`
        /// 故此字段设置不起作用
        /// </summary>
        public string name;
        #endregion

        #region 设置全局配置，不要轻易修改
        public int familyname_num_max = 500;
        public int name_num_max = 3;
        public int com_num_max = 2;
        public int rank_head_num = 3;
        #endregion


    }


}
