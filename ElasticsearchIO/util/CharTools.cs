using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticsearchIO.util
{
    public class CharTools
    {
        public static char ToHalfAngle(char c)
        {
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
            return c;
        }

        public static string ToHalfAngle(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            var sb = new StringBuilder();
            foreach (var c in name)
            {
                sb.Append(ToHalfAngle(c));
            }
            return sb.ToString();
        }
    }
}
