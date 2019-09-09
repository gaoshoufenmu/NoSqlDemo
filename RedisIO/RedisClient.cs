using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using StackExchange.Redis;

namespace QZ.Redis.Search
{
    public class RedisClient
    {
        #region 数据库相关常量

        public const string ComNameArea = "na:";
        /// <summary>
        /// 公司名反向索引键前缀
        /// </summary>
        public const string ComRevIdx = "ri:";
        /// <summary>
        /// 公司名重复的情况，公司名反向索引键前缀
        /// </summary>
        public const string ComRevIdxSet = "ris:";
        /// <summary>
        /// 公司股东键的后缀
        /// </summary>
        public const string ComDtlShare = "cd:gd:";       // slot 6358 (11)
        /// <summary>
        /// 公司成员键的后缀
        /// </summary>
        public const string ComDtlMember = "cd:sn:";      // slot 13867 (14)
        /// <summary>
        /// 自然人投资及东家联合键后缀
        /// </summary>
        public const string PerInvHost = "p:iv:";     // slot 12274  (14)
        /// <summary>
        /// 公司的投资键后缀
        /// </summary>
        public const string NameKeyInv = "nk:tz:";    // slot 2205 (10)

        public const string noticePrefix = "fy:gg:";
        public const string judgePrefix = "fy:ws:";
        public const string executePrefix = "fy:zx:";
        public const string dishonestPrefix = "fy:sx:";
        #endregion

        private StreamWriter _sw;
        
        private ManualResetEventSlim _mres = new ManualResetEventSlim(false);
        private object _lock = new object();
        /// <summary>
        /// 主力
        /// </summary>
        private ConnectionMultiplexer _manager;
        private string _connStr;

        private static RedisClient _instance = new RedisClient();
        public static RedisClient Instance { get { return _instance; } }
        public int _revIdxDB = 1;
        public int _entMbDB = 3;
        public int _entShDB = 2;
        public int _entInvDB = 4;
        public int _courtDB = 6;
        public int _personDB = 5;
        public int _entNameAreaDB = 7;

        private RedisClient()
        {
            var lines = File.ReadAllLines(AppDomain.CurrentDomain.BaseDirectory + "redisConfig.txt");
            var dir = AppDomain.CurrentDomain.BaseDirectory + "logs";//redis-conn-log_" + DateTime.Now.ToString("yyyyMM");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            _sw = new StreamWriter(dir + "/redis-conn-log_" + DateTime.Now.ToString("yyyyMM"), true);
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
                    case "redis-conn-str":
                        _connStr = value;
                        _manager = ConnectionMultiplexer.Connect(value, _sw);
                        _sw.Flush();
                        _mres.Set();
                        break;
                    case "rev-idx-no":
                        _revIdxDB = int.Parse(value);
                        break;
                    case "court-no":
                        _courtDB = int.Parse(value);
                        break;
                    case "ent-inv-no":
                        _entInvDB = int.Parse(value);
                        break;
                    case "ent-mb-no":
                        _entMbDB = int.Parse(value);
                        break;
                    case "ent-sh-no":
                        _entShDB = int.Parse(value);
                        break;
                    case "person-no":
                        _personDB = int.Parse(value);
                        break;
                    case "name-area-no":
                        _entNameAreaDB = int.Parse(value);
                        break;
                }
            }
        }
        public void ResetConn()
        {
            lock (_lock)
            {
                if (_manager == null || !_manager.IsConnected)
                {
                    _mres.Reset();

                    if (_manager != null)
                    {
                        _manager.Close();
                        _manager.Dispose();
                        _manager = null;
                    }

                    for(int i = 0; i < 10; i++)
                    {
                        try
                        {
                            _manager = ConnectionMultiplexer.Connect(_connStr, _sw);
                            _sw.Flush();
                            _mres.Set();
                            return;
                        }
                        catch (Exception e)
                        {
                            Thread.Sleep(500);
                            if (i == 9)
                            {
                                _mres.Set();
                                throw new Exception("faild 9 times when do redis re-connecting operation", e);
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Custom Redis Transient Error Detenction Strategy must have been implemented to satisfy Redis exceptions.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public bool IsTransient(Exception ex)
        {
            if (ex == null) return false;

            if (ex is TimeoutException) return true;

            if (ex is RedisServerException) return true;

            if (ex is RedisException) return true;

            if (ex is IndexOutOfRangeException) return true;

            if (ex.InnerException != null)
            {
                return IsTransient(ex.InnerException);
            }

            return false;
        }
        public T Exception_Wrapper<T>(Func<T> func)
        {
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    _mres.Wait();
                    return func();
                }
                catch (Exception e)
                {
                    if (!IsTransient(e) || i == 2)
                    {
                        throw e;
                    }

                    ResetConn();
                }
            }
            throw new Exception("faild 2 times when do redis search operation");
        }
        public void Exception_Wrapper(Action act)
        {
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    _mres.Wait();
                    act();
                    return;
                }
                catch (Exception e)
                {
                    if (!IsTransient(e) || i == 2)
                    {
                        throw e;
                    }
                    ResetConn();
                }
            }
            throw new Exception("faild 2 times when do redis search operation");
        }

        #region 反向索引数据库读写
        /// <summary>
        /// 反向索引数据库的读取
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public RedisValue[] IndexDBGetMany(RedisKey[] keys) => Exception_Wrapper(() => _manager.GetDatabase(_revIdxDB).StringGet(keys));

        public RedisValue IndexDBGet(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_revIdxDB).StringGet(key));
        #endregion

        #region 法院数据库
        public RedisValue[] CourtSetMembers(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_courtDB).SetMembers(key));

        public Task<RedisValue[]> CourtSetMembersAsync(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_courtDB).SetMembersAsync(key));

        #endregion

        /// <summary>
        /// 获取Set的Members
        /// </summary>
        /// <param name="key">key full name</param>
        /// <returns></returns>
        public RedisValue[] SetMembers(string key) => Exception_Wrapper(() => _manager.GetDatabase(_entInvDB).SetMembers(key));



        public RedisValue[] EntNameAreaGetBulk(RedisKey[] keys) => Exception_Wrapper(() => _manager.GetDatabase(_entNameAreaDB).StringGet(keys));

        public RedisValue[] EntInvSetMembers(string key) => Exception_Wrapper(() => _manager.GetDatabase(_entInvDB).SetMembers(key));

        public List<RedisValue[]> BareEntInvSetMembersMany(RedisKey[] keys)
        {
            var db = _manager.GetDatabase(_entInvDB);
            var tasks = new Task<RedisValue[]>[keys.Length];

            for (int i = 0; i < keys.Length; i++)
            {
                tasks[i] = db.SetMembersAsync(keys[i]);
            }
            Task.WaitAll(tasks);
            return tasks.Select(t => t.Result).ToList();
        }
        public List<RedisValue[]> EntInvSetMembersMany(RedisKey[] keys) => Exception_Wrapper(() => BareEntInvSetMembersMany(keys));

        public Task<RedisValue[]> SetMembersAsync(string key) => Exception_Wrapper(() => _manager.GetDatabase(_entInvDB).SetMembersAsync(key));

        public RedisValue[] SortedSetKeys(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_entShDB).SortedSetRangeByRank(key));

        public RedisValue[] SortedSetKeysDesc(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_entShDB).SortedSetRangeByRank(key, order: Order.Descending));

        public Task<RedisValue[]> SortedSetKeysAsync(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_entShDB).SortedSetRangeByRankAsync(key));

        public List<RedisValue[]> BareSortedSetKeysManyPl(RedisKey[] keys)
        {
            var db = _manager.GetDatabase(_entShDB);
            var tasks = new Task<RedisValue[]>[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                tasks[i] = db.SortedSetRangeByRankAsync(keys[i]);
            }
            Task.WaitAll();
            return tasks.Select(t => t.Result).ToList();
        }
        public List<RedisValue[]> SortedSetKeysManyPl(RedisKey[] keys) => Exception_Wrapper(() => BareSortedSetKeysManyPl(keys));

        public SortedSetEntry[] SortedSetRange(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_entShDB).SortedSetRangeByScoreWithScores(key));

        public SortedSetEntry[] SortedSetRangeDescend(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_entShDB).SortedSetRangeByScoreWithScores(key, order: Order.Descending));

        public List<SortedSetEntry[]> SortedSetRangeManyPl(RedisKey[] keys)
        {
            var db = _manager.GetDatabase(_entShDB);
            var tasks = new Task<SortedSetEntry[]>[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                tasks[i] = db.SortedSetRangeByRankWithScoresAsync(keys[i]);
            }
            Task.WaitAll();
            return tasks.Select(t => t.Result).ToList();
        }

        /// <summary>
        /// 获取单个公司的所有成员
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public RedisValue[] EntMbHashKeys(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_entMbDB).HashKeys(key));

        public List<RedisValue[]> BareEntMbHashKeysMany(RedisKey[] keys)
        {
            var db = _manager.GetDatabase(_entMbDB);
            var tasks = new Task<RedisValue[]>[keys.Length];
            for(int i = 0; i < keys.Length; i++)
            {
                tasks[i] = db.HashKeysAsync(keys[i]);
            }
            Task.WaitAll(tasks);
            return tasks.Select(t => t.Result).ToList();
        }
        public List<RedisValue[]> EntMbHashKeysMany(RedisKey[] keys) => Exception_Wrapper(() => BareEntMbHashKeysMany(keys));

        public RedisValue[] EntHashKeys(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_entMbDB).HashKeys(key));

        public Task<RedisValue[]> EntHashKeysAsync(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_entMbDB).HashKeysAsync(key));

        public HashEntry[] EntHashGetAll(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_entMbDB).HashGetAll(key));

        ///// <summary>
        ///// 求极值
        ///// </summary>
        ///// <param name="key"></param>
        ///// <param name="ascend"></param>
        ///// <returns></returns>
        //public SortedSetEntry BareSortedSetExtreme(RedisKey key, Order order)
        //{
        //    var db = _manager.GetDatabase();
        //    var entries = db.SortedSetRangeByScoreWithScores(key, order: order, take: 1);
        //    if (entries.Length > 0)
        //        return entries[0];
        //    return new SortedSetEntry();
        //}

        //public SortedSetEntry SortedSetExtreme(RedisKey key, Order order) => Exception_Wrapper(() => BareSortedSetExtreme(key, order));

        public SortedSetEntry BareEntShareSortedSetExtreme(RedisKey key, Order order)
        {
            var db = _manager.GetDatabase(_entShDB);
            var entries = db.SortedSetRangeByScoreWithScores(key, order: order, take: 1);
            if (entries.Length > 0)
                return entries[0];
            return new SortedSetEntry();
        }
        public SortedSetEntry EntShareSortedSetExtreme(RedisKey key, Order order) => Exception_Wrapper(() => BareEntShareSortedSetExtreme(key, order));

        public RedisValue[] EntShareSortedSetRangeByRank(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_entShDB).SortedSetRangeByRank(key));

        

        /// <summary>
        /// 公司成员信息
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public List<HashEntry[]> BareEntHashGetAllManyPl(RedisKey[] keys)
        {
            var db = _manager.GetDatabase(_entMbDB);
            var tasks = new Task<HashEntry[]>[keys.Length];

            for (int i = 0; i < keys.Length; i++)
            {
                tasks[i] = db.HashGetAllAsync(keys[i]);
            }
            Task.WaitAll(tasks);

            return tasks.Select(t => t.Result).ToList();
        }
        public List<HashEntry[]> EntHashGetAllManyPl(RedisKey[] keys) => Exception_Wrapper(() => BareEntHashGetAllManyPl(keys));

        #region Court
        public void CourtSetAddBulkPl(KeyValuePair<RedisKey, RedisValue[]>[] pairs)
        {
            var db = _manager.GetDatabase(_courtDB);
            var tasks = new Task[pairs.Length];

            for (int i = 0; i < pairs.Length; i++)
            {
                var p = pairs[i];
                tasks[i] = db.SetAddAsync(p.Key, p.Value);
            }
            Task.WaitAll(tasks);
        }

        public void CourtStringSetBulkPl(KeyValuePair<RedisKey, RedisValue>[] pairs) => Exception_Wrapper(() => _manager.GetDatabase(_courtDB).StringSet(pairs));
        #endregion

        #region reverse index
        public RedisValue[] RevIdxStringGetBulk(RedisKey[] keys) => Exception_Wrapper(() => _manager.GetDatabase(_revIdxDB).StringGet(keys));


        public bool RevIdxStringAddBulk(KeyValuePair<RedisKey, RedisValue>[] pairs, When when) => Exception_Wrapper(() => _manager.GetDatabase(_revIdxDB).StringSet(pairs, when));

        public void RevIdxSetAddBulk(KeyValuePair<RedisKey, RedisValue[]>[] pairs) => Exception_Wrapper(() => 
        {
            var db = _manager.GetDatabase(_revIdxDB);
            var tasks = new Task[pairs.Length];

            for (int i = 0; i < pairs.Length; i++)
            {
                tasks[i] = db.SetAddAsync(pairs[i].Key, pairs[i].Value);
            }
            Task.WaitAll(tasks);
        });
        #endregion

        #region enterprise
        public void EntSortedSetAddBulkPl(KeyValuePair<RedisKey, SortedSetEntry[]>[] pairs) => Exception_Wrapper(() =>
        {
            var db = _manager.GetDatabase(_entShDB);
            var tasks = new Task[pairs.Length];

            for (int i = 0; i < pairs.Length; i++)
            {
                tasks[i] = db.SortedSetAddAsync(pairs[i].Key, pairs[i].Value);
            }
            Task.WaitAll(tasks);
        });

        public void EntHashSetBulkPl(KeyValuePair<RedisKey, HashEntry[]>[] pairs) => Exception_Wrapper(() =>
        {
            var db = _manager.GetDatabase(_entMbDB);
            var tasks = new Task[pairs.Length];

            for (int i = 0; i < pairs.Length; i++)
            {
                tasks[i] = db.HashSetAsync(pairs[i].Key, pairs[i].Value);
            }
            Task.WaitAll(tasks);
        });

        public void EntInvSetAddBulk(KeyValuePair<RedisKey, RedisValue[]>[] pairs) => Exception_Wrapper(() =>
        {
            var db = _manager.GetDatabase(_entInvDB);
            var tasks = new Task[pairs.Length];

            for (int i = 0; i < pairs.Length; i++)
            {
                tasks[i] = db.SetAddAsync(pairs[i].Key, pairs[i].Value);
            }
            Task.WaitAll(tasks);
        });
        #endregion

        #region 自然人数据库
        /// <summary>
        /// 自然人数据库写
        /// </summary>
        /// <param name="pairs"></param>
        public void PersonHashSetBulkPl(KeyValuePair<RedisKey, HashEntry[]>[] pairs) => Exception_Wrapper(() =>
        {
            var db = _manager.GetDatabase(_personDB);
            var tasks = new Task[pairs.Length];

            for (int i = 0; i < pairs.Length; i++)
            {
                tasks[i] = db.HashSetAsync(pairs[i].Key, pairs[i].Value);
            }
            Task.WaitAll(tasks);
        });

        public RedisValue[] PersonHashGet(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_personDB).HashKeys(key));

        public HashEntry[] PersonHashGetAll(RedisKey key) => Exception_Wrapper(() => _manager.GetDatabase(_personDB).HashGetAll(key));

        public bool PersonHashSetRemove(RedisKey key, RedisValue field)
        {
            var db = _manager.GetDatabase(_personDB);
            return db.HashDelete(key, field);
        }

        public bool PersonHashSetRemove(RedisKey key)
        {
            var db = _manager.GetDatabase(_personDB);
            return db.KeyDelete(key);
        }

        public List<HashEntry[]> PersonHashGetAllManyPl(RedisKey[] keys) => Exception_Wrapper(() =>
        {
            var db = _manager.GetDatabase(_personDB);
            var tasks = new Task<HashEntry[]>[keys.Length];

            for (int i = 0; i < keys.Length; i++)
            {
                tasks[i] = db.HashGetAllAsync(keys[i]);
            }
            Task.WaitAll(tasks);

            return tasks.Select(t => t.Result).ToList();
        });

        public List<RedisValue[]> PersonHashKeysMany(RedisKey[] keys) => Exception_Wrapper(() =>
        {
            var db = _manager.GetDatabase(_personDB);
            var tasks = new Task<RedisValue[]>[keys.Length];

            for (int i = 0; i < keys.Length; i++)
            {
                tasks[i] = db.HashKeysAsync(keys[i]);
            }
            Task.WaitAll(tasks);

            return tasks.Select(t => t.Result).ToList();
        });

        #endregion
    }
}
