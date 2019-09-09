using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace ElasticsearchIO
{
    public class SearchPostHandler
    {
        public static ESWrapper<Person> PostHandle(ISearchResponse<Person> r)
        {
            var w = new ESWrapper<Person>() { took = r.Took, total = r.Total };
            w.docs = new List<DocWrapper<Person>>();
            w.aggs = GetAggs(r);

            foreach(var h in r.Hits)
            {
                var d = new DocWrapper<Person> { doc = h.Source, score = h.Score ?? 0 };
                d.hl = new Dictionary<string, string>();
                foreach(var hl in h.Highlights)
                {
                    d.hl[hl.Key] = hl.Value.Highlights.FirstOrDefault();
                }
            }
            return w;
        }

        private static List<Agg> GetAggs(ISearchResponse<Person> r)
        {
            var aggs = new List<Agg>();
            foreach(var a in r.Aggregations)
            {
                if (Enum.TryParse(a.Key, out PersonEnum e))
                {
                    var agg = new Agg() { items = new List<AggItem>(), name = a.Key };
                    aggs.Add(agg);
                    switch (e)
                    {
                        case PersonEnum.age:
                            var ages = r.Aggregations.Range(a.Key);
                            foreach(var b in ages.Buckets)
                            {
                                var ai = new AggItem();
                                ai.name = b.Key;
                                ai.count = b.DocCount;
                                agg.items.Add(ai);
                            }
                            break;
                        case PersonEnum.city:
                        case PersonEnum.isMan:
                            var cities = r.Aggregations.Terms(a.Key);
                            foreach(var b in cities.Buckets)
                            {
                                var ai = new AggItem();
                                ai.name = b.Key;
                                ai.count = b.DocCount??0;
                                agg.items.Add(ai);
                            }
                            break;
                        case PersonEnum.graduate:
                            var graduates = r.Aggregations.DateHistogram(a.Key);
                            foreach(var b in graduates.Buckets)
                            {
                                var ai = new AggItem();
                                ai.name = b.KeyAsString;
                                ai.count = b.DocCount ?? 0;
                                agg.items.Add(ai);
                            }
                            break;
                    }
                }
                
            }
            return aggs;
        }
    }
}
