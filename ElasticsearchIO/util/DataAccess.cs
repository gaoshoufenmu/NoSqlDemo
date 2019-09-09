using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;
using System.Threading.Tasks;

namespace ElasticsearchIO
{
    class DataAccess
    {
        public static string db_conn_str;

        public static List<T> SelectMany<T>(string querySql, string db_conn_str)
        {
            var list = new List<T>();
            try
            {
                using (var conn = new SqlConnection(db_conn_str))
                {
                    using(SqlDataAdapter sda = new SqlDataAdapter(querySql, conn))
                    {
                        DataSet ds = new DataSet();
                        sda.Fill(ds);
                        if (ds.Tables[0].Rows.Count > 0)
                        {
                            foreach (DataRow dr in ds.Tables[0].Rows)
                            {
                                list = GetEntityList<T>(ds.Tables[0].Rows);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Log_Error(e.Message + "\n\n" + e.StackTrace);
            }
            return list;
        }

        public static void Execute(string cmdSql, string db_conn_str)
        {
            try
            {
                using(var conn = new SqlConnection(db_conn_str))
                {
                    conn.Open();
                    using(var cmd = new SqlCommand(cmdSql, conn))
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch(Exception e)
            {
                Log.Log_Error(e.Message + "\n\n" + e.StackTrace);
            }
        }

        private static List<T> GetEntityList<T>(System.Data.DataRowCollection rows)
        {
            List<T> list = new List<T>();
            foreach (System.Data.DataRow dr in rows)
            {
                list.Add(GetEntity<T>(dr));
            }
            return list;
        }

        /// <summary>
        /// 将DataRow转换为类型T的实体对象
        /// </summary>
        /// <typeparam name="T">IEntity的派生类</typeparam>
        /// <param name="dr">DataRow</param>
        /// <returns>IEntity的派生类</returns>
        private static T GetEntity<T>(System.Data.DataRow dr)
        {
            T t = FastObjectFactory.CreateObjectFactory<T>();
            Type ent = ReflectionUtility.GetReflectionUtility.GetType<T>(t);
            PropertyInfo[] pi_arr = ent.GetProperties();
            object cv;
            foreach (PropertyInfo p in pi_arr)
            {
                cv = dr[p.Name];
                if (cv != DBNull.Value)
                {
                    //var gt = p.PropertyType.GetGenericTypeDefinition();
                    if (p.PropertyType.Equals(typeof(Nullable<int>)))
                    {
                        p.SetValue(t, Convert.ChangeType(dr[p.Name], Nullable.GetUnderlyingType(p.PropertyType)), null);
                    }
                    else
                        p.SetValue(t, Convert.ChangeType(dr[p.Name], p.PropertyType), null);
                }
            }
            return t;
        }
    }

    public static class FastObjectFactory
    {
        private static readonly Hashtable creatorCache = Hashtable.Synchronized(new Hashtable());

        private readonly static Type coType = typeof(CreateObject);
        public delegate object CreateObject();

        public delegate object GetValueDelegaet();
        public delegate void SetValueDelegate(object o);



        /// <summary>
        /// Create an object that will used as a 'factory' to the specified type T 
        /// 快速IL方法+缓存 创建对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreateObjectFactory<T>()
        {
            Type t = typeof(T);
            FastObjectFactory.CreateObject c = creatorCache[t] as FastObjectFactory.CreateObject;
            if (c == null)
            {
                lock (creatorCache.SyncRoot)
                {
                    c = creatorCache[t] as FastObjectFactory.CreateObject;
                    if (c == null)
                    {
                        DynamicMethod dynMethod = new DynamicMethod("DM$OBJ_FACTORY_" + t.Name, typeof(object), null, t);
                        ILGenerator ilGen = dynMethod.GetILGenerator();

                        ilGen.Emit(OpCodes.Newobj, t.GetConstructor(Type.EmptyTypes));
                        ilGen.Emit(OpCodes.Ret);
                        c = (CreateObject)dynMethod.CreateDelegate(coType);
                        creatorCache.Add(t, c);
                    }
                }
            }
            return (T)c.Invoke();
        }

        /// <summary>
        /// 普通方法创建对象+缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T CreateInstance<T>()
        {
            //Type t = typeof(T);
            //object ent = acCache[t];
            //if (ent == null)
            //{
            //    lock (acCache.SyncRoot)
            //    {
            //        ent = Activator.CreateInstance<T>();
            //        acCache.Add(t, ent);
            //    }
            //}
            //return (T)ent;

            //PropertyBuilder[] pb = (PropertyBuilder[])typeof(T).GetProperties();

            //Type t = typeof(T);
            //PropertyInfo[] p = t.GetProperties();


            return Activator.CreateInstance<T>();
        }

        /// <summary>
        /// 设置值的委托
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <param name="cName"></param>
        /// <returns></returns>
        public static SetValueDelegate CreateSetValueDelegate<T>(T t, string cName)
        {
            return (SetValueDelegate)Delegate.CreateDelegate(typeof(SetValueDelegate)
                , t
                , ReflectionUtility.GetReflectionUtility.GetType(t).GetProperty(cName).GetSetMethod());
        }

    }

    public sealed class ReflectionUtility
    {
        //字典类，用于缓存反射类型
        private static Dictionary<string, Type> _dic = null;

        //私有本类对象
        private static ReflectionUtility _ru = new ReflectionUtility();

        /// <summary>
        /// 访问锁
        /// </summary>
        private static object accessLockObj;

        //私有构造
        static ReflectionUtility()
        {
            _dic = new Dictionary<string, Type>();
            accessLockObj = new object();
        }

        /// <summary>
        /// 得到实例
        /// </summary>
        public static ReflectionUtility GetReflectionUtility
        {
            get { return _ru; }
        }

        /// <summary>
        /// 得到一个实体类的反射类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public Type GetType<T>(T t)
        {
            string classStr = t.ToString();
            if (_dic.ContainsKey(classStr))
                return _dic[classStr];
            lock (accessLockObj)
            {
                if (_dic.ContainsKey(classStr))
                    return _dic[classStr];
                else
                {
                    _dic.Add(classStr, typeof(T));
                    return _dic[classStr];

                }
            }

        }


    }
}
