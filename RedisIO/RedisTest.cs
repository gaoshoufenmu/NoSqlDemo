using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace QZ.Redis.Search
{
    public class RedisTest
    {
        public static void TestMap()
        {
            var name = "金兴资本有限公司";
            var code = "MA002C5H1";
            var area = "";
            var entMap = MapReader.GetEntMap2FmtJson(name, code, area);
            Console.WriteLine(entMap);
            Console.ReadLine();
        }
        public static void TestAll()
        {
            //var name = "小米科技有限责任公司";
            //var code = "551385082";
            //var area = "1101";
            //var name = "深圳前瞻资讯股份有限公司";
            //var code = "734185657";
            //var area = "4403";

            var name = "深圳市腾讯计算机系统有限公司";
            var code = "708461136";
            var area = "4403";

            // 投资族谱
            //var invCluster = MapReader.GetInvCluster(name, code, area);
            //// 股权结构
            //var stockStruct = MapReader.GetStockStructs(name, code, area);
            // 实际控制人
            //var actualControl = MapReader.GetActualControllers(name, code, area);
            //var ac = MapReader.GetStockController(name, code, area);
            // 企业图谱
            var entMap = MapReader.GetEntMap(name, code, area);
            // 疑似关系
            //var relation = MapReader.GetRelations(name, code, area);

        }


        public static void TestAll2Json()
        {
            //var name = "小米科技有限责任公司";
            //var code = "551385082";
            //var area = "1101";

            //var name = "江苏华西集团有限公司";
            //var code = "142232229";
            //var area = "3202";

            var name = "深圳前瞻资讯股份有限公司";
            var code = "734185657";
            var area = "4403";

            //var name = "深圳市腾讯计算机系统有限公司";
            //var code = "708461136";
            //var area = "4403";
            //// 投资族谱
            //Console.WriteLine("invest cluster");
            //Console.WriteLine(MapReader.GetInvCluster2FmtJson(name, code, area));
            //File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "json1.txt", MapReader.GetStockConstroller2FmtJson(name, code, area));
            ////// 股权结构
            ////Console.WriteLine("--  --  --  --  --  --  --  --  --  --  --  --  --  ");
            ////Console.WriteLine("stock structure");
            ////Console.WriteLine(MapReader.GetStockStructs2FmtJson(name, code, area));
            //// 实际控制人
            //Console.WriteLine("--  --  --  --  --  --  --  --  --  --  --  --  --  ");
            //Console.WriteLine("actual controllers");
            ////Console.WriteLine(MapReader.GetActualControllers2FmtJson(name, code, area));
            //Console.WriteLine(MapReader.GetStockConstroller2FmtJson(name, code, area));
            //// 企业图谱
            //Console.WriteLine("--  --  --  --  --  --  --  --  --  --  --  --  --  ");
            //Console.WriteLine("enterprise map");
            //File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "json.txt", MapReader.GetEntMap2FmtJson(name, code, area));
            // 疑似关系
            //Console.WriteLine("--  --  --  --  --  --  --  --  --  --  --  --  --  ");
            //Console.WriteLine("suspicious relations");
            ////Console.WriteLine(MapReader.GetRelations2FmtJson(name, code, area));
            //File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "json1.txt", MapReader.GetRelations2FmtJson(name, code, area));


            // 根据自然人获取其参与的公司
            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "getcombyperson.txt", MapReader.GetCompanyByPerson2FmtJson(code, "陈立科"));
        }

        public static void TestRelation()
        {
            var cname0 = "深圳前瞻资讯股份有限公司";    //小米科技有限责任公司  
            var cname1 = "苏州朗动网络科技有限公司";
            var pname0 = "陈恩";  // 雷军
            var pname1 = "陈立科"; //陈德强
            var pname2 = "刘瑞";

            var binRelation1 = new List<string>() { "西藏险峰管理咨询有限公司" };      //  "小米科技有限责任公司", "雷军"
            var binRelation2 = new List<string>() { "苏州朗动网络科技有限公司" }; // "苏州朗动网络科技有限公司", "陈德强"
            //var radis = int.Parse(Console.ReadLine());
            //MapReader.GetRelationMixMode(new List<string>() { }, new List<string>() { "雷军", "陈德强", "王伟", "陈超", "李林" }, 4);
            //
            var str = MapViewer.GetBinTrueRelationJson(binRelation1, binRelation2, 8);
            Console.WriteLine(str);
            //MapReader.GetRelation("小米科技有限责任公司", "苏州朗动网络科技有限公司", 3, "雷军", "陈德强");
            //MapViewer.GetMultiTrueRelation(new List<string>() { "深圳前瞻资讯股份有限公司", "深圳企查宝数据科技有限公司" }, new List<string>() { "陈立科", "陈恩" }, 4);
            //MapReader.GetComGroupsStep("陈涛", 0, 10);

            Console.WriteLine("PLEASE input path radis:");
            //MapViewer.GetMultiTrueRelation(new List<string>() { "小米科技有限责任公司", "苏州朗动网络科技有限公司" }, new List<string>() { "雷军", "陈德强" }, 8);
            //MapViewer.GetRelationMixMode(new List<string>(), new List<string>() { "雷军", "陈德强" }, 5);
            //MapReader.GetRelationMixMode(null, new List<string>() { "陈恩", "陈立科" }, 4);

            //for (int i = 0; i < 1000; i++)
            //{
            //    //MapReader.GetRelation("西藏险峰管理咨询有限公司", "苏州朗动网络科技有限公司", 4, "", "陈德强");
                
            //    MapReader.GetComGroupsStep("陈涛", 0, 10);
            //    MapReader.GetComGroupsStep("王伟", 0, 10);
            //}
            Console.ReadLine();
        }

        public static void TestPersonInvest()
        {
            //MapReader.GetComGroupsStep("汪少希", 0, 10);

            foreach (var com in RedisClient.Instance.PersonHashGet(RedisClient.PerInvHost + "汪少希"))
                Console.WriteLine(com);
            RedisClient.Instance.PersonHashSetRemove(RedisClient.PerInvHost + "汪少希");

            foreach (var com in RedisClient.Instance.PersonHashGet(RedisClient.PerInvHost + "汪少希"))
                Console.WriteLine(com);
            Console.WriteLine("over");
            Console.ReadLine();
        }
    }
}
