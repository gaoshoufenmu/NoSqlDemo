using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QZ.Demo.Model
{
    public class VCompany
    {
        public string Name { get; set; }
        public string Addr { get; set; }
        public string LawPerson { get; set; }
        public string Tel { get; set; }
        public string Weight { get; set; }
        public string Brand { get; set; }
        public string Code { get; set; }
        public string Status { get; set; }
        public string RegDate { get; set; }
        public string Bussiness { get; set; }
        public string Score { get; set; }
        public VCompany() { }

        public VCompany(string name, string addr, string lp, string tel)
        {
            Name = name;
            Addr = addr;
            LawPerson = lp;
            Tel = tel;
        }

        public static List<VCompany> Defaults = new List<VCompany>()
        {
            new VCompany("mobile", "北京", "xx", "10086"),
            new VCompany("<html><font color=\"#FF0000\">盛大</font></html>", "上海", "xx", "021xxxx"),
            new VCompany("neteasy", "广州", "丁磊", "020xxxx"),
            new VCompany("mobile", "北京", "xx", "10086"),
            new VCompany("<html><font color=\"#FF0000\">盛大</font></html>", "上海", "xx", "021xxxx"),
            new VCompany("neteasy", "广州", "丁磊", "020xxxx"),
            new VCompany("mobile", "北京", "xx", "10086"),
            new VCompany("<html><font color=\"#FF0000\">盛大</font></html>", "上海", "xx", "021xxxx"),
            new VCompany("neteasy", "广州", "丁磊", "020xxxx"),
            new VCompany("neteasy", "广州", "丁磊", "020xxxx")
        };

    }
}
