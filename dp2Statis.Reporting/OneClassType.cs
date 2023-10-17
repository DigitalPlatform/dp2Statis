using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DigitalPlatform.Text;

namespace dp2Statis.Reporting
{
    internal class OneClassType
    {
        /// <summary>
        /// 分类号 type。例如 clc
        /// </summary>
        public string ClassType = "";
        /// <summary>
        /// 类名细目。不允许元素为空字符串。如果 .Count == 0 ，表示不使用细目，统一截取第一个字符作为细目
        /// </summary>
        public List<string> Filters = new List<string>();

        // 根据名字表创建分类号分级结构
        /* 名字表形态如下(注意每行之间实际上是逗号间隔密排)
         * class_clc
         *  A
         *  B
         * class_ktf
         * class_rdf
         * */
        public static int BuildClassTypes(string strNameTable,
            out List<OneClassType> results,
            out string strError)
        {
            strError = "";

            results = new List<OneClassType>();
            List<string> lines = StringUtil.SplitList(strNameTable);
            OneClassType? current = null;
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line) == true)
                    continue;
                string strLine = line.TrimEnd();
                if (string.IsNullOrEmpty(strLine) == true)
                    continue;
                if (strLine[0] != ' ')
                {
                    current = new OneClassType();
                    results.Add(current);
                    current.ClassType = strLine.Trim();
                }
                else
                {
                    if (current == null)
                    {
                        strError = "第一行的第一字符不能为空格";
                        return -1;
                    }
                    string strText = strLine.Substring(1).Trim();
                    current.Filters.Add(strText);
                }
            }

            // 将 Filters 数组排序，大的在前
            // 这样让 I1 比 I 先匹配上
            foreach (OneClassType type in results)
            {
                type.Filters.Sort(
                    delegate (string s1, string s2)
                    {
                        return -1 * string.Compare(s1, s2);
                    });
            }

            return 0;
        }

        public static int IndexOf(List<OneClassType> types, string strTypeName)
        {
            int i = 0;
            foreach (OneClassType type in types)
            {
                if (type.ClassType == strTypeName)
                    return i;
                i++;
            }
            return -1;
        }
    }

}
