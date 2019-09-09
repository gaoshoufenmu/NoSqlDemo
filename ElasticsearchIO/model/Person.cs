using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace ElasticsearchIO
{
    [ElasticsearchType(IdProperty = "id")]
    public class Person
    {
        public string id { get; set; }
        public string name { get; set; }
        public int age { get; set; }
        public bool isMan { get; set; }
        public string company { get; set; }

        public List<string> friends { get; set; }

        public double salary { get; set; }
        public GeoLocation house { get; set; }

        public DateTime graduate { get; set; }

        public string city { get; set; }
    }

    public enum PersonEnum
    {
        none,
        id,
        name,
        age,
        isMan,
        company,
        friends,
        salary,
        house,
        graduate,
        city
    }
}
