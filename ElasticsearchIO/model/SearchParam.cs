using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticsearchIO
{
    public class SearchParam
    {
        public string index;
        public string type;
        public int from;
        public int size=10;

        public double lon;
        public double lat;
        public double distance;

        public List<PersonEnum> aggs;
        public List<PersonEnum> aggs_def = new List<PersonEnum>()
        {
            PersonEnum.age,
            PersonEnum.graduate,
            PersonEnum.isMan,
            PersonEnum.city
        };

        public bool isInclude;
        public List<PersonEnum> sources;

        public KeyValuePair<PersonEnum, List<string>> batch_cond;

        public string keyword;

        public bool isAsc;
        public PersonEnum sort = PersonEnum.graduate;
    }
}
