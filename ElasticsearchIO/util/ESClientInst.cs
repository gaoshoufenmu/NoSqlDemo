using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Nest;

namespace ElasticsearchIO
{
    public class ESClientInst
    {
        public const string Index = "data_index";
        public const string Type = "data_type";

        internal static string[] uris;
        public static ElasticClient Client { get; private set; }

        public static void Init()
        {
            if(Client == null)
            {
                Client = new ElasticClient(new ConnectionSettings(new StaticConnectionPool(uris.Select(u => new Uri(u)))));
            }
        }

        public static bool IsIndexExist(string index) => Client.IndexExists(index).Exists;

        public static bool IsTypeExist(string index, string type) => Client.TypeExists(index, type).Exists;

    }
}
