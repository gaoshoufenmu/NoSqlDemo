using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using StackExchange.Redis;

namespace QZ.Redis.Search
{
    class RedisClientLux
    {
        #region 数据库相关常量
        /// <summary>
        /// 公司名反向索引键前缀
        /// </summary>
        public const string ComRevIdx = "{ri}:";
        /// <summary>
        /// 公司名重复的情况，公司名反向索引键前缀
        /// </summary>
        public const string ComRevIdxSet = "{ris}:";
        /// <summary>
        /// 公司股东键的后缀
        /// </summary>
        public const string ComDtlShare = "{cd:gd}:";       // slot 6358 (11)
        /// <summary>
        /// 公司成员键的后缀
        /// </summary>
        public const string ComDtlMember = "{cd:sn}:";      // slot 13867 (14)
        /// <summary>
        /// 自然人投资及东家联合键后缀
        /// </summary>
        public const string PerInvHost = "{p:iv}:";     // slot 12274  (14)
        /// <summary>
        /// 公司的投资键后缀
        /// </summary>
        public const string NameKeyInv = "{nk:tz}:";    // slot 2205 (10)

        public const string NoticePrefix = "cache:notice:";
        public const string JudgePrefix = "cache:judge:";
        public const string ExecutePrefix = "cache:exec:";
        public const string DishonestPrefix = "cache:dish:";

        public static string noticePrefix = "fy:gg:";
        public static string judgePrefix = "fy:ws:";
        public static string executePrefix = "fy:zx:";
        public static string dishonestPrefix = "fy:sx:";
        #endregion


        /// <summary>
        /// 主力
        /// </summary>
        private ConnectionMultiplexer _manager;
        /// <summary>
        /// 辅助
        /// </summary>
        private ConnectionMultiplexer _auxManager;
        private int _revIdxDB = 0;
        private int _courtDB = 0;
        private static RedisClientLux _instance = new RedisClientLux();
        public static RedisClientLux Instance { get { return _instance; } }

        private RedisClientLux()
        {
            string connStr = null;
            string auxConnStr = null;
            bool isLocal = false;
            var lines = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "redisConfig.txt");
            for (int i = 0; i < lines.Length; i++)
            {
                var l = lines[i].TrimStart();
                if (string.IsNullOrEmpty(l) || l[0] == '#') continue;

                var fstSpaceIndex = l.IndexOf(' ');
                if (fstSpaceIndex < 0) continue;

                var key = l.Substring(0, fstSpaceIndex);
                var value = l.Substring(fstSpaceIndex + 1);

                switch (key)
                {
                    case "conn-str":
                        connStr = value;
                        break;
                    case "conn-str-aux":
                        auxConnStr = value;
                        break;
                    case "isLocal":
                        if (value[0] == 't')
                            isLocal = true;
                        break;
                    case "rev_idx_no":
                        _revIdxDB = int.Parse(value);
                        break;
                    case "court_no":
                        _courtDB = int.Parse(value);
                        break;
                }
            }
            _manager = ConnectionMultiplexer.Connect(connStr);
            if (isLocal)
            {
                _auxManager = ConnectionMultiplexer.Connect(auxConnStr);
                noticePrefix = NoticePrefix;
                executePrefix = ExecutePrefix;
                judgePrefix = JudgePrefix;
                dishonestPrefix = DishonestPrefix;
            }
            else
                _auxManager = _manager;
        }

        #region 反向索引数据库读写
        /// <summary>
        /// 反向索引数据库的写入
        /// </summary>
        /// <param name="pairs"></param>
        /// <param name="when"></param>
        /// <returns></returns>
        public bool IndexDBSetMany(KeyValuePair<RedisKey, RedisValue>[] pairs, When when)
        {
            var db = _auxManager.GetDatabase(_revIdxDB);
            return db.StringSet(pairs, when);
        }

        /// <summary>
        /// 反向索引数据库的读取
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public RedisValue[] IndexDBGetMany(RedisKey[] keys)
        {
            var db = _auxManager.GetDatabase(_revIdxDB);
            return db.StringGet(keys);
        }

        public RedisValue IndexDBGet(RedisKey key)
        {
            var db = _auxManager.GetDatabase(_revIdxDB);
            return db.StringGet(key);
        }

        /// <summary>
        /// 反向索引数据库Set值形式写入
        /// </summary>
        /// <param name="dict">Key不带键前缀，因为键的前缀已确定</param>
        /// <returns></returns>
        public void IndexDBSetSetMany(Dictionary<string, List<string>> dict)
        {
            var db = _auxManager.GetDatabase(_revIdxDB);
            foreach (var p in dict)
            {
                db.SetAdd(ComRevIdxSet + p.Key, p.Value.Select<string, RedisValue>(v => v).ToArray());
            }
        }

        /// <summary>
        /// 反向索引数据库Set值形式写入
        /// 使用管道
        /// </summary>
        /// <param name="dict"></param>
        public void IndexDBPlSetSetMany(Dictionary<string, List<string>> dict)
        {
            var db = _auxManager.GetDatabase(_revIdxDB);
            var tasks = new Task[dict.Count];
            int i = 0;
            foreach (var p in dict)
            {
                var t = db.SetAddAsync(ComRevIdxSet + p.Key, p.Value.Select<string, RedisValue>(v => v).ToArray());
                tasks[i++] = t;
            }
            Task.WaitAll(tasks);
        }
        #endregion

        #region 法院数据库
        public RedisValue[] CourtSetMembers(RedisKey key)
        {
            var db = _auxManager.GetDatabase(_courtDB);
            return db.SetMembers(key);
        }

        public Task<RedisValue[]> CourtSetMembersAsync(RedisKey key)
        {
            var db = _auxManager.GetDatabase(_courtDB);
            return db.SetMembersAsync(key);
        }

        #endregion

        /// <summary>
        /// 获取Set的Members
        /// </summary>
        /// <param name="key">key full name</param>
        /// <returns></returns>
        public RedisValue[] SetMembers(string key)
        {
            var db = _manager.GetDatabase();
            return db.SetMembers(key);
        }

        public Task<RedisValue[]> SetMembersAsync(string key)
        {
            var db = _manager.GetDatabase();
            return db.SetMembersAsync(key);
        }

        /// <summary>
        /// 获取SortedSet的Keys（不含Score）
        /// </summary>
        /// <param name="key">key full name</param>
        /// <returns></returns>
        public RedisValue[] SortedSetKeys(RedisKey key)
        {
            var db = _manager.GetDatabase();
            return db.SortedSetRangeByRank(key);
        }

        public Task<RedisValue[]> SortedSetKeysAsync(RedisKey key)
        {
            var db = _manager.GetDatabase();
            return db.SortedSetRangeByRankAsync(key);
        }
        public List<RedisValue[]> SortedSetKeysManyPl(RedisKey[] keys)
        {
            var db = _manager.GetDatabase();
            var tasks = new Task<RedisValue[]>[keys.Length];
            for(int i = 0; i < keys.Length; i++)
            {
                tasks[i] = db.SortedSetRangeByRankAsync(keys[i]);
            }
            Task.WaitAll();
            return tasks.Select(t => t.Result).ToList();
        }

        public SortedSetEntry[] SortedSetRange(RedisKey key)
        {
            var db = _manager.GetDatabase();
            return db.SortedSetRangeByScoreWithScores(key);
        }

        public RedisValue[] HashKeys(RedisKey key)
        {
            var db = _manager.GetDatabase();
            return db.HashKeys(key);
        }

        public Task<RedisValue[]> HashKeysAsync(RedisKey key)
        {
            var db = _manager.GetDatabase();
            return db.HashKeysAsync(key);
        }

        public HashEntry[] HashGetAll(RedisKey key)
        {
            var db = _manager.GetDatabase();
            return db.HashGetAll(key);
        }

        /// <summary>
        /// 求极值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ascend"></param>
        /// <returns></returns>
        public SortedSetEntry SortedSetExtreme(RedisKey key, Order order)
        {
            var db = _manager.GetDatabase();
            var entries = db.SortedSetRangeByScoreWithScores(key, order: order, take: 1);
            if (entries.Length > 0)
                return entries[0];
            return new SortedSetEntry();
        }

        public List<HashEntry[]> HashGetAllManyPl(RedisKey[] keys)
        {
            var db = _manager.GetDatabase();
            var tasks = new Task<HashEntry[]>[keys.Length];

            for(int i = 0; i < keys.Length; i++)
            {
                tasks[i] = db.HashGetAllAsync(keys[i]);
            }
            Task.WaitAll(tasks);

            return tasks.Select(t => t.Result).ToList();
        }
    }
}
