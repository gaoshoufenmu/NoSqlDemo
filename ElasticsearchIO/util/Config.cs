using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ElasticsearchIO
{
    public class Config
    {
        public static void Init()
        {
            foreach(var line in File.ReadLines(AppDomain.CurrentDomain.BaseDirectory+"config.txt"))
            {
                if (string.IsNullOrWhiteSpace(line) || line[0]=='#') continue;

                var segs = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                switch(segs[0])
                {
                    case "db_conn_str":
                        DataAccess.db_conn_str = segs[1];
                        break;
                    case "es_conn_str":
                        ESClientInst.uris = segs[1].Split(new[] { ',',' ' }, StringSplitOptions.RemoveEmptyEntries);
                        break;
                }
            }
        }
    }
}
