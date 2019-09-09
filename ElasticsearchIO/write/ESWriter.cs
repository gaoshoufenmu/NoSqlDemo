using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nest;

namespace ElasticsearchIO.write
{
    public class ESWriter
    {
        public static bool CreateIndex(string index) => ESClientInst.Client.CreateIndex(index, cid => cid
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(0)
                .Analysis(ana => ana
                    .Analyzers(anal => anal
                        .Standard("sstd", std => std.MaxTokenLength(1))     // 单字符切分，主要用于英文，因为中文 std 分词就是单字符分词
                        .Pattern("sep", p => p.Pattern(@"[-\.\|,\s]"))
                        .Pattern("ascii", p => p.Pattern(@"\p{ASCII}"))
                    )
                )
            )
        ).IsValid;

        public static bool CreateMap(string index, string type) =>
            ESClientInst.Client.Map<Person>(m => m.Index(index).Type(type).Properties(p => p
                  .Keyword(k => k.Name(n => n.id))
                  .Keyword(k => k.Name(n => n.name))
                  .Number(n => n.Name(nn => nn.age).Type(NumberType.Integer))
                  .Number(n => n.Name(nn => nn.salary).Type(NumberType.Double))
                  .Boolean(b => b.Name(n => n.isMan))
                  .GeoPoint(g=>g.Name(n => n.house))
                  .Keyword(k => k.Name(n => n.friends))
                  .Date(d => d.Name(n => n.graduate))
                  .Keyword(k => k.Name(n => n.city))
                  .Keyword(t => t.Name(n => n.company)
                      .Fields(f => f.Text(k => k.Name("max").Analyzer("ik_max_word").SearchAnalyzer("ik_max_word"))
                                  .Text(tt => tt.Name("std"))))     // 中文单字符切分，英文则是单词切分（即 空格切分）
            )).IsValid;

        public static IBulkResponse BulkIndex<T>(IEnumerable<T> ts, string index, string type) where T : class =>
            ESClientInst.Client.Bulk(b => b.Index(index).Type(type).IndexMany(ts));

        public static IBulkResponse BulkPartialUpdate(IEnumerable<Person> ps, string index, string type) =>
            ESClientInst.Client.Bulk(b => b.Index(index).Type(type).UpdateMany<Person, object>(ps, (u, p) => u.Id(p.id).Doc(new
            {
                age = p.age,
                friends = p.friends,
                company = p.company
            })));


        /**
         * 对于某条 Person 数据，如果逐步 friends，可使用此方法
         * */
        public static IBulkResponse BulkAddFriends(IEnumerable<Person> ps, string index, string type) =>
            ESClientInst.Client.Bulk(b => b.Index(index).Type(type).UpdateMany<Person, object>(ps, (u, p) => u.Id(p.id).Script(s => s
                    .Source(@"
if(ctx._source.containsKey('friends')) {
    for(int i = 0; i < params.firends.length; i++) {
        String s = params.friends.get(i);
        if(!ctx._source.friends.contains(s)) 
            ctx._source.friends.add(s);
    }
} else {
    ctx._source.friends = params.tags;
}
")
                .Lang("painless").Params(new Dictionary<string, object>() { ["friends"] = p.friends }))));

        /**
         * 逐步删除某些 friends
         * */
        public static IBulkResponse BulkRemoveFriends(IEnumerable<Person> ps, string index, string type) =>
            ESClientInst.Client.Bulk(b => b.Index(index).Type(type).UpdateMany<Person, object>(ps, (u, p) => u.Id(p.id).Script(s => s
                   .Source(@"
if(ctx._source.containsKey('friends')) {
    for(int i = 0; i < params.friends.length; i++) {
        String s = params.friends.get(i);
        if(ctx._source.friends.contains(s))
            ctx._source.friends.remove(s);
}
")
            .Lang("painless").Params(new Dictionary<string, object>() { ["friends"] = p.friends }))));


        public static IDeleteResponse DeleteById(string id, string index, string type) =>
            ESClientInst.Client.Delete<Person>(id, s => s.Index(index).Type(type));
        public static IDeleteByQueryResponse DeleteByQuery(string name, string index, string type) =>
            ESClientInst.Client.DeleteByQuery<Person>(d => d.Index(index).Type(type).Query(q => q.Term(t => t.Field(f => f.name).Value(name))));

        public static IDeleteByQueryResponse DeleteManyByQuery(IEnumerable<string> names, string index, string type) =>
            ESClientInst.Client.DeleteByQuery<Person>(d => d.Index(index).Type(type).Query(q => q.Terms(t => t.Field(f => f.name).Terms(names))));


        public static bool CreateMap_familyname(string index, string type)
        {
            var resp = ESClientInst.Client.Map<familyname>(m => m.Index(index).Type(type).Properties(p => p
                .Keyword(k => k.Name(n => n.ccode))
                .Keyword(k => k.Name(n => n.cname))
                .Keyword(k => k.Name(n => n.carea))
                .Keyword(k => k.Name(n => n.cmarea))
                .Keyword(k => k.Name(n => n.member))
                .Keyword(k => k.Name(n => n.fn_faren))
                .Keyword(k => k.Name(n => n.fn_gudong))
                .Keyword(k => k.Name(n => n.fn_member))
                .Keyword(k => k.Name(n => n.creg_captical))
                .Date(d => d.Name(n => n.creg_date))
                .Number(n => n.Name(nm => nm.cweight).Type(NumberType.Float))
                .Number(n => n.Name(nm => nm.cstatus).Type(NumberType.Byte))

          //.Nested<Tag>(n => n.Name(nm => nm.tags).Properties(prop => prop
          //    .Keyword(k => k.Name(nm => nm.name))
          //    .Number(num => num.Name(nm => nm.score).Type(NumberType.Double))))
          ));
            return resp.IsValid;
        }
    }
}
