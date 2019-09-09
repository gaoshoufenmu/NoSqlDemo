using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace QZ.Redis.Search
{
    public class Cryptor
    {
        #region fields
        /// <summary>
        /// 标准的人名最大长度
        /// 大于等于此长度的认为是非标准人名
        /// </summary>
        private static int maxNameLen = 5;
        /// <summary>
        /// 混淆
        /// </summary>
        private static string interMix = "李灵黛冷文卿柳兰歌秦水支李念儿文彩柳婵诗顾莫言任水寒金磨针丁玲珑凌霜华水笙景茵梦容柒雁肖永涛陈庆韩保标郑广锐陈更群郑广锐吴芳刘杰峰王克需范玉斌崔洲范玉斌许胜智张建朝巴豪磊王瑞霞";
        /// <summary>
        /// 基本编码，由于要作为有效url地址一部分，故只用了如下字符
        /// </summary>
        private static string _base = "0123456789defghiabcjkopqrstulmnvwxyz";
        /// <summary>
        /// 非标准人名（长度大于5）必须以中文开头，否则出错
        /// </summary>
        public static readonly int[] primes =
        {
            131, 137, 139, 149, 151, 157, 163, 167, 173,
            179, 181, 191, 193, 197, 199, 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281
        };
        #endregion


        public static string Md5_16(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new Exception("input can not be empty");

            var md5 = new MD5CryptoServiceProvider();
            return BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(input)))
                .Replace("-", "").ToLower().Substring(8, 16);
        }

        public static string Md5(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) throw new Exception("input can not be empty");

            var md5 = new MD5CryptoServiceProvider();
            return BitConverter.ToString(md5.ComputeHash(Encoding.Default.GetBytes(input)))
                .Replace("-", "").ToLower();
        }

        #region 加密方法
        public static string EncryptNameFix(string name)
        {
            var sb = new StringBuilder(maxNameLen * 3);
            var list = new List<char>(maxNameLen);
            var delta = maxNameLen - name.Length;
            var hash = Math.Abs(name.GetHashCode());
            var r = hash % primes.Length;
            var q = hash / primes.Length;
            var r2 = q % primes.Length;
            var r3 = (q + r) % primes.Length;
            

            if (delta == 1)
            {
                list.Add((char)primes[r]);
                list.AddRange(name);
            }
            else if(delta == 2)
            {
                list.Add((char)primes[r]);
                list.Add((char)primes[r2]);
                list.AddRange(name);
            }
            else if(delta == 3)
            {
                list.Add((char)primes[r]);
                list.Add((char)primes[r2]);
                list.Add((char)primes[r3]);
                list.AddRange(name);
            }
            else
            {
                list.AddRange(name);
            }

            for (int i = 0; i < list.Count; i++)
            {
                int v = list[i];
                for (int j = 0; j < 3; j++)
                {
                    var r1 = v % 36;
                    sb.Append(_base[r1]);
                    v /= 36;
                }
            }
            return sb.ToString();
        }

        public static string DecryptNameFix(string name)
        {
            if (name.Length % 3 != 0)
                throw new Exception("crypted text length error: must be divided exactly by 3");

            var len = name.Length / 3;
            var arr = new char[len];
            var sb = new StringBuilder(len);
            bool hasHead = false;
            for (int i = 0; i < name.Length; i += 3)
            {
                var index1 = _base.IndexOf(name[i]);
                var index2 = _base.IndexOf(name[i + 1]);
                var index3 = _base.IndexOf(name[i + 2]);

                var v = index3 * 36 * 36 + index2 * 36 + index1;
                //arr[j++] = (char)v;
                if (v > 500 && !hasHead)
                {
                    hasHead = true;
                }
                if(hasHead)
                {
                    sb.Append((char)v);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 加密人名
        /// 人名长度必须大于等于2
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string EncryptName(string name)
        {
            var sb = new StringBuilder(maxNameLen * 3);
            var delta = maxNameLen - name.Length;
            var list = new List<char>(maxNameLen);
            if(delta > 0)
            {
                var rand = new Random(Guid.NewGuid().GetHashCode());
                list.Add((char)(primes[rand.Next(primes.Length)] * delta));
                if(delta > 1)
                {
                    
                    for(int i = 1; i < delta; i++)
                    {
                        list.Add(name[i - 1]);
                        list.Add(interMix[rand.Next(interMix.Length)]);
                        if (i == name.Length) break;
                    }
                }
                var rem = name.Length + 1 - delta;
                if (rem > 0)
                {
                    list.AddRange(name.Substring(delta - 1, rem));
                }
            }
            else
            {
                list.AddRange(name);
            }

            for(int i = 0; i < list.Count; i++)
            {
                int v = list[i];
                for(int j = 0; j < 3; j++)
                {
                    var r = v % 36;
                    sb.Append(_base[r]);
                    v /= 36;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 解密人名
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string DecryptName(string name)
        {
            if (name.Length % 3 != 0)
                throw new Exception("crypted text length error: must be divided exactly by 3");

            var len = name.Length / 3;
            var arr = new char[len];
            var sb = new StringBuilder(len);
            int j = 0;
            for(int i = 0; i < name.Length; i+=3)
            {
                var index1 = _base.IndexOf(name[i]);
                var index2 = _base.IndexOf(name[i + 1]);
                var index3 = _base.IndexOf(name[i + 2]);

                var v = index3 * 36 * 36 + index2 * 36 + index1;
                arr[j++] = (char)v;
            }

            var delta = -1;
            int fVal = arr[0];
            for(int i = 0; i < primes.Length; i++)
            {
                if(fVal % primes[i] == 0)
                {
                    var q = fVal / primes[i];
                    if(q < 5)
                    {
                        delta = q;
                        break;
                    }
                }
            }

            if (delta >= 0)
            {
                for (int i = 0; i < delta; i++)
                {
                    j = (i << 1) + 1;
                    if (j >= arr.Length) break;
                    sb.Append(arr[j]);
                }
                j++;
                if(j < arr.Length)
                {
                    for(int k = j; k < arr.Length; k++)
                    {
                        sb.Append(arr[k]);
                    }
                }
            }
            else
                sb.Append(arr);

            return sb.ToString();
        }

        /// <summary>
        /// 加密机构代码
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string EncryptCode(string code) => HuffmanTree.Tree.Encode2Str(code);

        /// <summary>
        /// 解密机构代码
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string DecryptCode(string code)
        {
            var decrypted = HuffmanTree.Tree.Decode(code);
            if (decrypted.Length > 9)
                decrypted = decrypted.Substring(0, 9);
            return decrypted;
        }

        /// <summary>
        /// 自然人名与公司机构代码的加密，两者之间使用 "-"连接
        /// </summary>
        /// <param name="name"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string EncryptNameCode(string name, string code) => EncryptNameFix(name) + "-" + EncryptCode(code);
        #endregion

        #region test material
        public static string _name = @"
王保峰
张冬梅
张改法
徐华品
崔中州
刘安来
袁琳
李育红
肖金荣
陈占强
权红光
李丽萍
李爱娟
巩瑞松
穆春华
牛素峰
牛笑灵
贺志强
丁长江
李晓龙
阎红云
靳海澄
刘三军
王琪
杨薇潇
杜威
唐佳
李二勇
郭庭跃
祝建平
王颖
臧广军
万方
李成娜
霍新亮盛辉
张海新华
郭香华·王海涛·孟三明
王海涛·孟三明
";

        public static string _code = @"
101781950
102210260
601226777
601495336
602313063
603930688
104762923
607455757
608785512
108404592
611344531
109802912
110585265
61757280X
617890250
115134383
622447472
625338271
119M71881
630202133
122P04624
631306188
123617617
126005974
63435O920
128967755
132376448
133852557
135B542C6
N3ABCE5MO
137765709
660138910
66038357X
66058438X
600006836
600481266
101573430
101929572
60087932X
";
        #endregion

        public static void TestNameEncrypted()
        {
            var names = _name.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach(var name in names)
            {
                var encrypted = EncryptNameFix(name);
                var decrypted = DecryptNameFix(encrypted);

                if (name != decrypted)
                    throw new Exception("not equal");

                Console.WriteLine(name + "|" + encrypted + "|" + decrypted);
            }
        }

        public static void TestCodeEncrypted()
        {
            var codes = _code.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var tree = HuffmanTree.Tree;

            foreach(var code in codes)
            {
                var encrypted = tree.Encode2Str(code);
                var decrypted = tree.Decode(encrypted);
                if (decrypted.Length > 9)
                    decrypted = decrypted.Substring(0, 9);

                if (code != decrypted)
                    throw new Exception("not equal");

                Console.WriteLine(code + "|" + encrypted + "|" + decrypted);
            }          
        }
    }
}
