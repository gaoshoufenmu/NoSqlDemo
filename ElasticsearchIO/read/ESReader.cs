using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace ElasticsearchIO
{
    public class ESReader
    {
        public static ISearchResponse<Person> SearchHouse(SearchParam p)
        {
            var resp = ESClientInst.Client.Search<Person>(s => s.Index(p.index).Type(p.type).From(p.from).Take(p.size)
                .Sort(sd => GetHourseSortDescriptor(sd, p))
                .Source(sr => GetSourceFilter(sr, p))
                .Aggregations(agg => GetAggContainer(agg, p))
                .Query(q => q.GeoDistance(g => g.Distance(p.distance, DistanceUnit.Kilometers)
                                                .DistanceType(GeoDistanceType.Arc)
                                                .Field(f => f.house)
                                                .Location(p.lat, p.lon)))
            );
            return resp;
        }

        public static ISearchResponse<Person> SearchMany(SearchParam p)
        {
            var resp = ESClientInst.Client.Search<Person>(s => s.Index(p.index).Type(p.type).From(p.from).Take(p.size)
                .Sort(sd => GetHourseSortDescriptor(sd, p))
                .Source(sr => GetSourceFilter(sr, p))
                .Aggregations(agg => GetAggContainer(agg, p))
                .Query(q => q.Terms(t=>t.Field(p.batch_cond.Key.ToString()).Terms(p.batch_cond.Value)))
            );
            return resp;
        }

        public static ISearchResponse<Person> SearchByCom(SearchParam p)
        {
            var resp = ESClientInst.Client.Search<Person>(s => s.Index(p.index).Type(p.type).From(p.from).Take(p.size)
                .Sort(sd => GetHourseSortDescriptor(sd, p))
                .Source(sr => GetSourceFilter(sr, p))
                .Aggregations(agg => GetAggContainer(agg, p))
                .Highlight(hl => GetHLDescriptor(hl, p))
                .Query(q => q.DisMax(d => d.Queries(dq => dq.Term(t=>t.Field(f=>f.company).Value(p.keyword).Boost(100)),
                                                    dq => dq.MatchPhrase(m=>m.Field("company.max").Query(p.keyword).Boost(50)),
                                                    dq => dq.MatchPhrase(m=>m.Field("company.std").Query(p.keyword).Boost(1)))))
            );
            return resp;
        }

        public static ISearchResponse<Person> Search_Scroll(SearchParam p)
        {
            /**
             * if p.from >= 10000, we can only use this methods to get data
             * p.size must be less then 10000, and the better choices are 10, 20, 50, 100, 200, 500, 1000, 2000, 5000
             * */
            if (p.from % p.size != 0) throw new Exception("p.from must be integrally divided by p.size");
            var discard_count = p.from / p.size;
            int count=0;

            ISearchResponse<Person> resp = null;
            resp = ESClientInst.Client.Search<Person>(s => s.Scroll("1m").Index(p.index).Type(p.type).Take(p.size)
                .Query(q => q.Match(m => m.Field("company.max").MinimumShouldMatch(MinimumShouldMatch.Percentage(80)).Query(p.keyword)))
                .Sort(sd => p.isAsc ? sd.Ascending(p.sort.ToString()) : sd.Descending(p.sort.ToString())));

            string scroll_id = resp.ScrollId;

            while(count < discard_count && resp.Documents.Any() && scroll_id != null)
            {
                count++;

                resp = ESClientInst.Client.Scroll<Person>("1m", scroll_id);
                ESClientInst.Client.ClearScroll(cs => cs.ScrollId(scroll_id));
                scroll_id = resp.ScrollId;
            }
            return resp;
        }


        private static ISourceFilter GetSourceFilter(SourceFilterDescriptor<Person> sr, SearchParam p)
        {
            if (p.isInclude)
            {
                if (p.sources == null || p.sources.Count == 0)
                    return sr.ExcludeAll();
                else
                    return sr.Includes(inc => inc.Fields(p.sources.Select(s => s.ToString()).ToArray()));
            }
            else
            {
                if (p.sources == null || p.sources.Count == 0)
                    return sr.IncludeAll();
                else
                    return sr.Excludes(exc => exc.Fields(p.sources.Select(s => s.ToString()).ToArray()));
            }
        }

        private static HighlightDescriptor<Person> GetHLDescriptor(HighlightDescriptor<Person> hl, SearchParam p)
        {
            return hl.PreTags("<b>")
                    .PostTags("</b>")
                    .Fields(f => f.Field("company.std"),
                            f => f.Field("company.max"),
                            f => f.Field(fld=>fld.company));
        }

        private static IAggregationContainer GetAggContainer(AggregationContainerDescriptor<Person> agg, SearchParam p)
        {
            if (p.aggs != null)
            {
                foreach(var a in p.aggs)
                {
                    switch(a)
                    {
                        case PersonEnum.age:
                            agg.Range("age", r => r.Field(f => f.age).Ranges(rs => rs.To(20),
                                                                         rs => rs.From(20).To(30),
                                                                         rs => rs.From(30).To(40),
                                                                         rs => rs.From(40).To(50),
                                                                         rs => rs.From(50).To(60),
                                                                         rs => rs.From(60)));
                            break;
                        case PersonEnum.graduate:
                            agg.DateHistogram("graduate", d => d.Field(f => f.graduate)
                                                                .Interval(DateInterval.Year)
                                                                .MinimumDocumentCount(1)
                                                                .ExtendedBounds("1980", "2050"));
                            break;
                        case PersonEnum.isMan:
                            agg.Terms("isMan", t => t.Field(f => f.isMan));
                            break;
                        case PersonEnum.city:
                            agg.Terms("city", t => t.Field(f => f.city));
                            break;
                    }
                }
            }
            return agg;
        }

        private static SortDescriptor<Person> GetHourseSortDescriptor(SortDescriptor<Person> sd, SearchParam p)
        {
            return sd.GeoDistance(g => g.DistanceType(GeoDistanceType.Arc)
                                      .Points(new GeoLocation(p.lat, p.lon))
                                      .Field(f => f.house)
                                      .Order(SortOrder.Ascending));
        }


        /**
         * statistics
         * 
         * */

        /// <summary>
        /// 给定公司关键词，按毕业年份统计平均薪水
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public ISearchResponse<Person> AverageSalaryByYear(SearchParam p)
        {
            var resp = ESClientInst.Client.Search<Person>(s => s.Index(p.index).Type(p.type).Take(0)
                .Query(q => q.MatchPhrase(m => m.Field("company.max").Query(p.keyword)))
                .Aggregations(agg => agg
                    .DateHistogram($"average_salary_year", dh => dh
                        .Field(f => f.graduate)
                        .Interval(DateInterval.Year)
                        .Aggregations(sa => sa
                            .Average("average_salary", ave => ave
                                .Field(f => f.salary)
                                //.Script(scr => scr.Source("doc['salary']").Lang("expression"))
                                )
                            )
                        )
                    )
                );
            return resp;
        }

        /// <summary>
        /// 按城市统计平均薪水
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public ISearchResponse<Person> AverageSalaryByCity(SearchParam p)
        {
            var resp = ESClientInst.Client.Search<Person>(s => s.Index(p.index).Type(p.type).Take(0)
                .Aggregations(agg => agg
                    .Terms("city", t => t
                        .Field(f => f.city)
                        .Size(100)
                        .MinimumDocumentCount(50)
                        .Aggregations(sa => sa
                            .Average("average_salary", ave => ave
                                .Field(f => f.salary))
                        )
                    )
                    .AverageBucket("ave_sal_city_buck", ab => ab.BucketsPath("city>average_salary"))
                )
            );
            return resp;
        }

        #region FamilyName
        public static FamilyNamePack FamilyNameSearch(FamilyNameSearchParam fi)
        {
            if (DateTime.Now.Hour >= 0 && DateTime.Now.Hour < 6)
            {
                fi.ESIndex = "familyname_v1";
            }
            else
            {
                fi.ESIndex = "familyname_v0";
            }

            var res = new FamilyNamePack();
            var s = new SearchDescriptor<familyname>().Index(fi.ESIndex).Type(fi.ESType).Size(0);
            if (!string.IsNullOrWhiteSpace(fi.areacode))
            {
                s.Query(q => q.Bool(b => b.Filter(f => f.Term(t => t.Field(fi.areacode.Length == 2 ? "cmarea" : "carea").Value(fi.areacode)))));
            }
            var fns = new List<string>();
            ISearchResponse<familyname> r;
            bool fn_flag = false;
            if (string.IsNullOrWhiteSpace(fi.Familyname))
            {
                s.Aggregations(agg => GetAgg_fn_FamilyName(agg, fi));
                r = ESClientInst.Client.Search<familyname>(s);
                var count = r.Aggs.Terms("fn").Buckets.Count;
                res.totalpage = (count % fi.pg_size == 0) ? count / fi.pg_size : count / fi.pg_size + 1;

                if (r.Aggregations.ContainsKey("carea"))
                {
                    var areas = r.Aggs.Terms("carea");
                    res.Area2Num = areas.Buckets.ToDictionary(b => b.Key, b => (int)b.DocCount);
                }
                foreach (var aa in r.Aggs.Terms("fn").Buckets.Skip(fi.pg_index * fi.pg_size).Take(fi.pg_size))
                {
                    fns.Add(aa.Key);
                    var fnl = new FamilyName_List() { Familyname = aa.Key, total = (int)(aa.DocCount) };
                    fnl.list = new List<FN_Name_List>();
                    res.list.Add(fnl);
                }
                //fns.AddRange(r.Aggs.Terms("fn").Buckets.Skip(fi.pg_index * fi.pg_size).Take(fi.pg_size).Select(b => b.Key));
                s = new SearchDescriptor<familyname>().Index(fi.ESIndex).Type(fi.ESType).Size(0);
                //if (!string.IsNullOrWhiteSpace(fi.areacode))
                //{
                //    s.Query(q => q.Bool(b => b.Filter(f => f.Term(t => t.Field(fi.areacode.Length == 2 ? "cmarea" : "carea").Value(fi.areacode)))));
                //}
            }
            else
            {
                fn_flag = true;
                fns.Add(fi.Familyname);
                res.list.Add(new FamilyName_List() { Familyname = fi.Familyname, list = new List<FN_Name_List>() });
            }
            s.Query(q =>
            {
                QueryContainer qc = new QueryContainer();
                if (!string.IsNullOrWhiteSpace(fi.areacode))
                {
                    qc &= q.Bool(b => b.Filter(f => f.Term(t => t.Field(fi.areacode.Length == 2 ? "cmarea" : "carea").Value(fi.areacode))));
                }
                if (fns.Count > 0)
                {
                    qc &= q.Terms(t => t.Field("fn_" + fi.ptype.ToString()).Terms(fns));
                }
                return qc;
            });
            s.Aggregations(agg => GetAgg_name_FamilyName(agg, fi, fns));
            r = ESClientInst.Client.Search<familyname>(s);
            if (r.Aggregations.ContainsKey("carea"))
            {
                var areas = r.Aggregations.Terms("carea");
                res.Area2Num = areas.Buckets.ToDictionary(b => b.Key, b => (int)b.DocCount);
            }
            var names = new List<string>(30);
            var fnr = r.Aggregations.Filters("fn");
            var rank_head_flag = fi.pg_index == 0 && !fn_flag;
            for (var i = 0; i < res.list.Count; ++i)
            {
                var l = res.list[i];
                if (fnr.ContainsKey(l.Familyname))
                {
                    var agg = (Nest.SingleBucketAggregate)fnr.NamedBucket(l.Familyname);
                    var buck = (BucketAggregate)agg.Aggregations["name"];
                    var items = fn_flag ? buck.Items.Skip(fi.pg_index * fi.pg_size).Take(fi.pg_size) : buck.Items;
                    if (i < fi.rank_head_num && rank_head_flag || fn_flag)
                    {
                        foreach (KeyedBucket<object> item in items)
                        {
                            var nm = (string)item.Key;
                            var fn_name = new FN_Name_List() { name = nm, total = (int)(item.DocCount) };
                            l.list.Add(fn_name);
                            names.Add(nm);
                        }
                    }
                    else
                    {
                        var item = items.FirstOrDefault() as KeyedBucket<object>;
                        if (item != null)
                        {
                            var nm = (string)item.Key;
                            var fn_name = new FN_Name_List() { name = nm, total = (int)(item.DocCount) };
                            l.list.Add(fn_name);
                            names.Add(nm);
                        }
                    }

                    if (fn_flag)
                    {
                        l.total = buck.Items.Sum(it => (int)((KeyedBucket<object>)it).DocCount);
                    }
                }
            }

            var mr = ESClientInst.Client.MultiSearch(ms => GetMultiSearch_FamilyName(ms, names, fi));
            foreach (var p in res.list)
            {
                foreach (var k in p.list)
                {
                    var rr = mr.GetResponse<familyname>(k.name);
                    k.list = rr.Documents.Select(d => new ComMini() { code = d.ccode, name = d.cname, area = string.IsNullOrWhiteSpace(d.carea) ? d.cmarea : d.carea }).ToList();
                }
            }
            return res;
        }

        private static IMultiSearchRequest GetMultiSearch_FamilyName(MultiSearchDescriptor ms, List<string> names, FamilyNameSearchParam fi)
        {
            var flag = string.IsNullOrWhiteSpace(fi.areacode);
            foreach (var n in names)
            {
                ms.Search<familyname>(n, s => s.Index(fi.ESIndex).Type(fi.ESType)
                 .Size(2)
                 .Sort(st => st.Descending(d => d.cweight))
                 .Source(src => src.Includes(inc => inc.Fields("cname", "ccode", "cmarea", "carea")))
                 .Query(q => flag ? q.Term(t => t.Field(fi.ptype.ToString()).Value(n))
                         : (q.Term(t => t.Field(fi.ptype.ToString()).Value(n)) & q.Term(t => t.Field(fi.areacode.Length == 2 ? "cmarea" : "carea").Value(fi.areacode))))
                );
            }
            return ms;
        }

        private static IPromise<INamedFiltersContainer> GetNamedFilters_FamilyName(NamedFiltersContainerDescriptor<familyname> f, string field, List<string> names)
        {
            foreach (var n in names)
            {
                f.Filter(n, ff => ff.Term(t => t.Field(field).Value(n)));
            }
            return f;
        }

        private static QueryContainer GetContainer_FamilyName(QueryContainerDescriptor<familyname> q, FamilyNameSearchParam ci)
        {
            QueryContainer qc = new QueryContainer();
            if (!string.IsNullOrWhiteSpace(ci.Familyname))
            {
                qc = q.Term(t => t.Field("fn_" + ci.ptype.ToString()).Value(ci.Familyname));
            }
            if (!string.IsNullOrWhiteSpace(ci.areacode))
            {
                if (ci.areacode.Length == 2)
                {
                    qc &= q.Term(t => t.Field("cmarea").Value(ci.areacode));
                }
                else if (ci.areacode.Length == 4)
                {
                    qc &= q.Term(t => t.Field("carea").Value(ci.areacode));
                }
            }
            return qc;
        }

        private static IAggregationContainer GetAgg_fn_FamilyName(AggregationContainerDescriptor<familyname> agg, FamilyNameSearchParam ci)
        {
            if (string.IsNullOrWhiteSpace(ci.areacode))
                agg.Terms("carea", t => t
                    .Field("cmarea").Size(32));
            else if (ci.areacode.Length == 2)
                agg.Terms("carea", t => t
                    .Field("carea").Size(24));

            agg.Terms("fn", t => t
                .Field("fn_" + ci.ptype.ToString())
                .Size(ci.familyname_num_max));
            return agg;
        }
        private static IAggregationContainer GetAgg_name_FamilyName(AggregationContainerDescriptor<familyname> agg, FamilyNameSearchParam ci, List<string> fns = null)
        {
            var size = ci.pg_index == 0 ? ci.name_num_max : 1;
            if (!string.IsNullOrWhiteSpace(ci.Familyname))
                size = ci.familyname_num_max * ci.name_num_max * ci.com_num_max;
            agg.Filters("fn", fs => fs
               .NamedFilters(nf => GetNamedFilters_FamilyName(nf, "fn_" + ci.ptype.ToString(), fns))
               .Aggregations(ca => ca.Terms("name", t => t.Field(ci.ptype.ToString()).Size(size))));

            if (!string.IsNullOrWhiteSpace(ci.Familyname))
            {
                if (string.IsNullOrWhiteSpace(ci.areacode))
                    agg.Terms("carea", t => t
                        .Field("cmarea").Size(32));
                else if (ci.areacode.Length == 2)
                    agg.Terms("carea", t => t
                        .Field("carea").Size(24));
            }
            return agg;
        }
        #endregion


        public void Search_StatByFN(string index)
        {
            var resp = ESClientInst.Client.Search<familyname>(s => s.Index(index).Type(nameof(familyname)).Size(0)
                .Aggregations(agg => agg
                    .Terms("cmarea", t => t
                        .Field("cmarea")
                        .Size(32)
                        )
                    .Terms("fn_member", t => t
                        .Field("fn_member")
                        .Size(500)
                    )
                    )
            );
            int pg_index = 2;
            int pg_size = 10;
            var fns = resp.Aggregations.Terms("fn_member").Buckets.Skip(pg_index * pg_size).Take(pg_size).Select(b => b.Key).ToList();
            resp = ESClientInst.Client.Search<familyname>(s => s.Index(index).Type(nameof(familyname)).Size(0)
                .Aggregations(agg => agg.Filters("fn_member", fs => fs
                     .NamedFilters(nf => GetNamedFilters(nf, "fn_member", fns)
                                           )
                     .Aggregations(ca => ca.Terms("member", t => t.Field("member").Size(pg_index == 0 ? 3 : 1).CollectMode(TermsAggregationCollectMode.DepthFirst)))
                    ))
                );

            var names = new List<string>();
            var fnss = resp.Aggregations.Filters("fn_member");
            foreach (var p in fnss)
            {
                var agg = (SingleBucketAggregate)p.Value;
                var buck = (BucketAggregate)agg.Aggregations["member"];
                foreach (KeyedBucket<object> item in buck.Items)
                {
                    //var fn_name = new Model.FN_Name_List() { name = item.Key, total = (int)(item.DocCount) };
                    //l.list.Add(fn_name);
                    names.Add((string)item.Key);
                }
            }
            resp = ESClientInst.Client.Search<familyname>(s => s.Index(index).Type(nameof(familyname)).Size(0)
                .Aggregations(agg => agg.Filters("member", fs => fs
                     .NamedFilters(nf => GetNamedFilters(nf, "member", names))
                     .Aggregations(ca => ca.TopHits("com", t => t.Size(2)))
                    ))
                );

            var r = ESClientInst.Client.MultiSearch(ms => GetMultiSearch(ms, names, index));
        }

        private IPromise<INamedFiltersContainer> GetNamedFilters(NamedFiltersContainerDescriptor<familyname> f, string field, List<string> names)
        {
            foreach (var n in names)
            {
                f.Filter(n, ff => ff.Term(t => t.Field(field).Value(n)));
            }
            return f;
        }

        private IMultiSearchRequest GetMultiSearch(MultiSearchDescriptor ms, List<string> names, string index)
        {
            foreach (var n in names)
            {
                ms.Search<familyname>(n, s => s.Index(index).Type(nameof(familyname)).Size(2).Query(q => q.Bool(b => b.Filter(f => f.Term(t => t.Field("member").Value(n))))));
            }
            return ms;
        }

        /**
         * 按姓氏统计
         * */
        public void Search_StatByFN2(string index)
        {
            var resp = ESClientInst.Client.Search<familyname>(s => s.Index(index).Type(nameof(familyname)).Size(0)
                .Aggregations(agg => agg
                    .Terms("cmarea", t => t
                        .Field("cmarea")
                        .Size(32)
                        )
                    .Terms("fn_member", t => t
                        .Field("fn_member")
                        //.MinimumDocumentCount(2)
                        .Size(1000)


                        .CollectMode(TermsAggregationCollectMode.DepthFirst)
                        .Order(o => o.CountDescending())
                        //.Order(TermsOrder.CountDescending)
                                //.Aggregations(aa => aa.
                                //    Terms("member", tt => tt
                                //        .Field("member")
                                //        .Size(1)

                                //        .CollectMode(TermsAggregationCollectMode.DepthFirst)
                                //        //.Order(TermsOrder.CountDescending)
                                //        //.Aggregations(aaa => aaa.TopHits("tophits", at => at.Size(2)))
                                //        ))
                                ))
            );
            int pg_index = 2;
            int pg_size = 5;
            var fns = resp.Aggs.Terms("fn_member").Buckets.Skip(pg_index * pg_size).Take(pg_size).Select(b => b.Key).ToArray();
            resp = ESClientInst.Client.Search<familyname>(s => s.Index(index).Type(nameof(familyname)).Size(0)
                .Query(q => q.Bool(b => b.Filter(f => f.Terms(ts => ts.Field("fn_member").Terms(fns)))))
                .Aggregations(a => a
                    .Terms("fn_member", t => t
                        .Field("fn_member")

                        //.MinimumDocumentCount(2)
                        .Size(5)
                        .CollectMode(TermsAggregationCollectMode.DepthFirst)
                        .Order(o => o.CountDescending())
                        //.Order(TermsOrder.CountDescending)
                                .Aggregations(aa => aa.
                                    Terms("member", tt => tt
                                        .Field("member")
                                        .Size(5)

                                        .CollectMode(TermsAggregationCollectMode.DepthFirst)
                                .Order(o => o.CountDescending())
                                //.Order(TermsOrder.CountDescending)
                                //.Aggregations(aaa => aaa.TopHits("tophits", at => at.Size(2)))

                                ))
                                ))
            );

            //resp = ESClientInst.Client.Search<Familyname>(s => s.Index(index).Type(nameof(Familyname)).Size(0)
            //    .Query(q => q.Bool(b => b.Filter(f => f.Terms(ts => ts.Field("fn_member").Terms(fns)))))
            //    .Aggregations(a => a
            //        .Filters("fn_member", fs => fs
            //            .AnonymousFilters(af => af.Term(t => t.Field("fn_member").Value(fns[0])), 
            //                              af => af.Term(t=>t.Field("fn_member").Value(fns[1])),
            //                              af=>af.Term(t=>t.Field("fn_member").Value(fns[2]))))
            //            .TopHits("member", th => th.Size(pg_index==0?3:1))
            //            )
            //        .Terms("fn_member", t => t
            //            .Field("fn_member")
            //            //.MinimumDocumentCount(2)
            //            .Size(5)
            //            .CollectMode(TermsAggregationCollectMode.DepthFirst)
            //            .Order(TermsOrder.CountDescending)
            //                    .Aggregations(aa => aa.
            //                        Terms("member", tt => tt
            //                            .Field("member")
            //                            .Size(pg_index == 0 ? 2 : 1)

            //                            .CollectMode(TermsAggregationCollectMode.DepthFirst)
            //                    .Order(TermsOrder.CountDescending)
            //                    //.Aggregations(aaa => aaa.TopHits("tophits", at => at.Size(2)))

            //                    ))
            //                    ))
            //);
        }

        public void Search_StatByFN(string fn, string index)
        {
            var resp = ESClientInst.Client.Search<familyname>(s => s.Index(index).Type(nameof(familyname)).Size(0)
                .Query(q => q.Bool(b => b.Filter(f => f.Term(t => t.Field("fn_member").Value(fn)))))
                .Aggregations(agg => agg
                    .Terms("cmarea", t => t
                        .Field("cmarea")
                        )
                    .Terms("member", t => t
                        .Field("member")
                        .Size(8000)
                        //.Aggregations(aa => aa.TopHits("tophits", th => th.Size(2)))
                        )
                    .TopHits("tophits", th => th.Size(2))));

        }

        public void Search_StatByFN(string fn, string cmarea, string index)
        {
            var resp = ESClientInst.Client.Search<familyname>(s => s.Index(index).Type(nameof(familyname)).Size(0)
                .Query(q => q.Bool(b => b.Filter(f => f.Term(t => t.Field("fn_member").Value(fn)) & f.Term(t => t.Field("cmarea").Value(cmarea)))))
                .Aggregations(agg => agg
                    .Terms("carea", t => t
                        .Field("carea")
                        )
                    .Terms("member", t => t
                        .Field("member")
                        .Size(8000)
                        //.Aggregations(aa => aa.TopHits("tophits", th => th.Size(2)))
                        )
                    .TopHits("tophits", th => th.Size(2))));

        }

        
    }
}
