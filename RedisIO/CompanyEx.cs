using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QZ.Redis.Search
{
    public static class CompanyEx
    {
        /// <summary>
        /// 规范化公司名
        /// </summary>
        /// <param name="comName"></param>
        /// <returns></returns>
        public static string NormalizeComName(this string comName)
        {
            var sb = new StringBuilder(comName.Length);
            var buffer = new StringBuilder();
            int tailInvalidIndex = -1;
            bool hasOpenBracket = false;
            bool innerValid = false;         // 括号内容是否有效
            bool curHasCom = false;
            for (int i = 0; i < comName.Length; i++)
            {
                var c = comName[i];
                if (c < '\u4e00' || c > '\u9fd5')
                {
                    if (tailInvalidIndex == -1)     // 如果为初始值，表示前面都是有效的，从当前位置处出现无效字符
                        tailInvalidIndex = i;

                    if (c == '（')
                        c = '(';
                    else if (c == '）')
                        c = ')';
                    else if (c == '【')
                        c = '[';
                    else if (c == '】')
                        c = ']';
                    else
                    {
                        // 转半角
                        if (c == 12288)
                            c = ' ';
                        else if (c > 65280 && c < 65375)    // 全角字母转半角
                            c = (char)(c - 65248);
                    }



                    if (c == '(' || c == '[')       // 设置开括号，这里不考虑嵌套括号的情况（如果考虑的话，可以使用状态机实现）
                        hasOpenBracket = true;

                    if (hasOpenBracket && (c == ')' || c == ']'))   // 如果前面有开括号，则如果尾部有闭括号，则闭货号之前的segment需要保留
                    {
                        tailInvalidIndex = -1;
                        hasOpenBracket = false;
                        // 遇到闭括号，将缓存刷入
                        if (!curHasCom || innerValid)
                            sb.Append(buffer).Append(c);
                        buffer.Clear();
                    }
                    else
                    {
                        // 非中文字符加入缓存
                        buffer.Append(c);
                    }
                }
                else
                {
                    if (c == '司' && i > 0 && comName[i - 1] == '公')
                        curHasCom = true;

                    tailInvalidIndex = -1;      // 是中文字符，则重置为初始值
                    if (hasOpenBracket)         // 如果有开括号，则加入缓存
                    {
                        buffer.Append(c);
                        if (c == '日' || c == '月')
                        {
                            if (!ContainsDate(buffer))      // 不包含日期，则认为括号内容有效
                                innerValid = true;
                        }
                    }
                    else
                    {
                        if (buffer.Length > 0)  // 当前没有开括号，且有缓存
                        {
                            sb.Append(buffer);
                            buffer.Clear();
                        }
                        sb.Append(c);
                    }
                }
            }

            return sb.ToString();
        }

        public static string ToHalfAngle(this string name)
        {
            var sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (c == '（')
                    c = '(';
                else if (c == '）')
                    c = ')';
                else if (c == '【')
                    c = '[';
                else if (c == '】')
                    c = ']';
                else
                {
                    // 转半角
                    if (c == 12288)
                        c = ' ';
                    else if (c > 65280 && c < 65375)    // 全角字母转半角
                        c = (char)(c - 65248);
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static bool ContainsDate(StringBuilder sb)
        {
            for (int j = sb.Length - 2; j > -1; j--)
            {
                if (sb[j] == '年' || sb[j] == '月')
                    return true;
            }
            return false;
        }

        public const string ComEnds = "厂社司堂心行院店校部园所处站局馆厦城学府寓队场团庄家室厅楼库宫屋会房";
        public static bool IsCompany(string input)
        {
            if (input.Length > 5)
            {
                var lastChar = input[input.Length - 1];
                if (ComEnds.Contains(lastChar))
                    return true;
                if (input.Contains("有限合伙"))
                    return true;
                if (input.Contains("有限公司"))
                    return true;
                if (input.Contains("企业"))
                    return true;
            }
            return false;
        }


        public static int IsCompany_1(string input)
        {
            if (input.Length > 4)
            {
                if (input.Contains("流通股"))
                    return 3;
                var lastChar = input[input.Length - 1];
                if (ComEnds.Contains(lastChar))
                    return 1;
                if (input.Contains("有限合伙"))
                    return 1;

                if (input.Contains("企业"))
                    return 1;
            }
            return 2;
        }
    }
}
