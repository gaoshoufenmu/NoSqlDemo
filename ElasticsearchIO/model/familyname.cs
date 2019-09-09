using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace ElasticsearchIO
{
    [ElasticsearchType(IdProperty = "ccode")]
    public class familyname
    {
        public string cname;
        public string ccode { get; set; }
        public string carea;
        public string cmarea;

        public float cweight;

        public string creg_captical;
        public DateTime creg_date;
        public byte cstatus;

        public string fn_faren;
        public List<string> fn_gudong;
        public List<string> fn_member;


        public List<string> member;
    }
}
