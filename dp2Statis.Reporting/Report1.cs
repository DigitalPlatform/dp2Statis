﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Xml;
using System.Data;

using Microsoft.EntityFrameworkCore;
using static MoreLinq.Extensions.LeftJoinExtension;

using DigitalPlatform.IO;
using DigitalPlatform.Text;
using dp2Statis.Reporting;
using DocumentFormat.OpenXml.Spreadsheet;

namespace DigitalPlatform.LibraryServer.Reporting
{
    public class Report
    {
        // return:
        //      -1  出错
        //      0   没有创建文件(因为输出的表格为空)
        //      >=1   成功创建文件
        public static int BuildReport(LibraryContext context,
    Hashtable param_table,
    ReportWriter writer,
    string strOutputFileName)
        {
            string strDateRange = param_table["dateRange"] as string;

            string strStartDate = "";
            string strEndDate = "";
            if (string.IsNullOrEmpty(strDateRange) == false)
            {
                // 将日期字符串解析为起止范围日期
                // throw:
                //      Exception
                DateTimeUtil.ParseDateRange(strDateRange,
                    out strStartDate,
                    out strEndDate);
                if (string.IsNullOrEmpty(strEndDate) == true)
                    strEndDate = strStartDate;
            }

            Hashtable macro_table = new Hashtable();

            macro_table["%daterange%"] = strDateRange;
            macro_table["%library%"] = param_table["libraryCode"] as string;

            switch (writer.Algorithm)
            {
                case "101":
                    return BuildReport_101(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                // TODO: 增加 102 表
                case "102":
                    return BuildReport_102(context,
param_table,    // 注意其中包含 strNameTable
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                // TODO: 增加 102 表
                case "111":
                    return BuildReport_111(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "121":
                    return BuildReport_121(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "131":
                    return BuildReport_131(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "141":
                    return BuildReport_141(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "201":
                    return BuildReport_201(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "202":
                    return BuildReport_202(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "212":
                    return BuildReport_212(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                // 注: 213 表已经废止
                case "301":
                    return BuildReport_301(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "302":
                    return BuildReport_302(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "411":
                    return BuildReport_411(context,
param_table,
strStartDate,
strEndDate,
"setOrder",
writer,
macro_table,
strOutputFileName);
                case "412":
                    return BuildReport_412(context,
param_table,
strStartDate,
strEndDate,
"setOrder",
writer,
macro_table,
strOutputFileName);
                case "421":
                    return BuildReport_421(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "422":
                    return BuildReport_422(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "431":
                    return BuildReport_431(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "432":
                    return BuildReport_432(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "441":
                    return BuildReport_441(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "442":
                    return BuildReport_442(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "443":
                    return BuildReport_443(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "451":
                    return BuildReport_411(context,
param_table,
strStartDate,
strEndDate,
"setIssue",
writer,
macro_table,
strOutputFileName);
                case "452":
                    return BuildReport_412(context,
param_table,
strStartDate,
strEndDate,
"setIssue",
writer,
macro_table,
strOutputFileName);
                case "471":
                    return BuildReport_471(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "472":
                    return BuildReport_472(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "481":
                    return BuildReport_481(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "482":
                    return BuildReport_482(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "491":
                    return BuildReport_491(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "492":
                    return BuildReport_492(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
                case "493":
                    return BuildReport_493(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);
            }

            throw new Exception($"算法 {writer.Algorithm} 没有找到");
        }

        static string GetLibraryCode(string location)
        {
            return StringUtil.ParseTwoPart(location, "/")[0];
        }

        // 获得一个参数值。带有检查参数是否具备的功能
        public static string GetParam(Hashtable param_table,
            string name,
            bool throwException = true)
        {
            if (param_table.ContainsKey(name) == false)
            {
                if (throwException)
                    throw new ArgumentException($"尚未指定 param_table 中的 {name} 参数");
                return "";
            }

#pragma warning disable CS8603 // 可能返回 null 引用。
            return param_table[name] as string;
#pragma warning restore CS8603 // 可能返回 null 引用。
        }

        // 获取对象操作量。按分类。注意操作者既可能是工作人员，也可能是读者
        // parameters:
        public static int BuildReport_493(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            string classType = GetParam(param_table, "classType");

            // string classType = param_table["classType"] as string;

            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");
            
            macro_table["%library%"] = libraryCode;
            macro_table["%class%"] = classType;

            string libraryCode0 = libraryCode;
            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);


#if NO
            var items = context.GetResOpers
                .Where(b => b.Operation == "getRes"
                && (libraryCode == "*" || b.LibraryCode.IndexOf(libraryCode) != -1)
                && string.Compare(b.Date, strStartDate) >= 0
                && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin(
                context.Patrons,
                oper => oper.Operator,
                patron => patron.Barcode,
                (oper, patron) => new
                {
                    PatronLibraryCode = patron == null ? null : patron.LibraryCode,
                    oper.Operator,
                    oper.Size,
                    oper.XmlRecPath
                }
                )
                .LeftJoin(
                context.Users,
                oper => oper.Operator,
                user => user.ID,
                (oper, user) => new
                {
                    oper.PatronLibraryCode,
                    UserLibraryCode = user == null ? null : user.LibraryCodeList,
                    oper.Operator,
                    oper.XmlRecPath,
                    GetCount = 1,
                    GetSize = Convert.ToInt64(oper.Size),
                    TotalCount = 1,
                }
                )
                .Where(x => (x.UserLibraryCode != null && (libraryCode == "*" || x.UserLibraryCode.IndexOf(libraryCode) != -1))
                || (x.PatronLibraryCode != null && (libraryCode0 == "*" || x.PatronLibraryCode == libraryCode0)))
                .LeftJoin(
                context.Keys,
                item => item.XmlRecPath,
                key => key.BiblioRecPath,
                (item, key) => new
                {
                    key.Type,
                    Class = string.IsNullOrEmpty(key.Text) ? "" : key.Text.Substring(0, 1),
                    item.GetCount,
                    item.GetSize,
                }
            )
            .Where(x => x.Type == classType)
            .GroupBy(x => x.Class)
            .Select(g => new
            {
                Class = g.Key,
                GetCount = g.Sum(x => x.GetCount),
                GetSize = g.Sum(x => x.GetSize),
            })
                .OrderBy(x => x.Class)
            .ToList();
#endif

            var items = context.GetResOpers
                .Where(b => b.Operation == "getRes"
                    && (libraryCode == "*" || b.LibraryCode.Contains(libraryCode))
                    && string.Compare(b.Date, strStartDate) >= 0
                    && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
                    context.Patrons,
                    oper => oper.Operator,
                    patron => patron.Barcode,
                    (oper, patron) => new
                    {
                        // 多次 join 时的典型写法，传递对象而不是属性：
                        oper,
                        patron, // 注意可能为空
                    })
                .LeftJoin1(
                    context.Users,
                    j1 => j1.oper.Operator,
                    user => user.ID,
                    (j1, user) => new
                    {
                        // 因为后面 where 马上要用到，所以给一个具体的名字
                        PatronLibraryCode = j1.patron == null ? null : j1.patron.LibraryCode,
                        UserLibraryCode = user == null ? null : user.LibraryCodeList,

                        // 向后传递对象
                        j1.oper,
                        // user,
                    })
                .Where(x => (x.UserLibraryCode != null && (libraryCode == "*" || x.UserLibraryCode.IndexOf(libraryCode) != -1))
                    || (x.PatronLibraryCode != null && (libraryCode0 == "*" || x.PatronLibraryCode == libraryCode0)))
                .LeftJoin1(
                    context.Keys,
                    j2 => new { recpath = j2.oper.XmlRecPath, type = classType, index = 0 },
                    key => new { recpath = key.BiblioRecPath, type = key.Type, index = key.Index },
                    (j2, key) => new
                    {
                        Class = string.IsNullOrEmpty(key.Text) ? "" : key.Text.Substring(0, 1),
                        GetCount = 1,
                        GetSize = string.IsNullOrEmpty(j2.oper.Size) ? 0 : Convert.ToInt64(j2.oper.Size),
                    }
                )
                .GroupBy(x => x.Class)
                .Select(g => new
                {
                    Class = g.Key,
                    GetCount = g.Sum(x => x.GetCount),
                    GetSize = g.Sum(x => x.GetSize),
                })
                .OrderBy(x => x.Class)
                .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 获取对象操作量。按操作者。注意操作者既可能是工作人员，也可能是读者
        // parameters:
        public static int BuildReport_492(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;
            
            string libraryCode0 = libraryCode;
            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            var items = context.GetResOpers
                .Where(b => b.Operation == "getRes"
                && (libraryCode == "*" || b.LibraryCode.Contains(libraryCode))
                && string.Compare(b.Date, strStartDate) >= 0
                && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
                context.Patrons,
                oper => oper.Operator,
                patron => patron.Barcode,
                (oper, patron) => new
                {
                    PatronLibraryCode = patron == null ? null : patron.LibraryCode,
                    // Operation = oper.Action,
                    oper.Operator,
                    // oper.Action,
                    // oper.XmlRecPath,
                    Size = oper.Size,
                }
                )
                .LeftJoin1(
                context.Users,
                oper => oper.Operator,
                user => user.ID,
                (oper, user) => new
                {
                    oper.PatronLibraryCode,
                    UserLibraryCode = user == null ? null : user.LibraryCodeList,
                    // oper.Action,
                    oper.Operator,
                    // oper.Size,
                    // oper.XmlRecPath,
                    GetCount = 1,
                    GetSize = string.IsNullOrEmpty(oper.Size) ? 0 : Convert.ToInt64(oper.Size),
                    TotalCount = 1,
                }
                )
                .Where(x => (x.UserLibraryCode != null && (libraryCode == "*" || x.UserLibraryCode.IndexOf(libraryCode) != -1))
                || (x.PatronLibraryCode != null && (libraryCode0 == "*" || x.PatronLibraryCode == libraryCode0)))
            .GroupBy(x => x.Operator)
            .Select(g => new
            {
                Operator = g.Key,
                GetCount = g.Sum(x => x.GetCount),
                GetSize = g.Sum(x => x.GetSize),
                TotalCount = g.Sum(x => x.TotalCount),
            })
                .LeftJoin1(
                context.Patrons,
                oper => oper.Operator,
                patron => patron.Barcode,
                (oper, patron) => new
                {
                    oper.Operator,
                    oper.GetCount,
                    oper.GetSize,
                    oper.TotalCount,
                    Name = patron == null ? null : patron.Name,
                    Department = patron == null ? null : patron.Department,
                }
                ).OrderBy(x => x.Operator)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);

            return nRet + 1;
        }

        // 获取对象流水
        // parameters:
        public static int BuildReport_491(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;
            
            string libraryCode0 = libraryCode;
            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            var items = context.GetResOpers
                .Where(b => b.Operation == "getRes"
                && (libraryCode == "*" || b.LibraryCode.Contains(libraryCode))
                && string.Compare(b.Date, strStartDate) >= 0
                && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
                context.Patrons,
                oper => oper.Operator,
                patron => patron.Barcode,
                (oper, patron) => new
                {
                    PatronLibraryCode = patron == null ? null : patron.LibraryCode,
                    Operation = oper.Action,
                    oper.OperTime,
                    oper.Operator,
                    oper.Action,
                    oper.XmlRecPath,
                    oper.Size,
                    oper.ObjectID,
                }
                )
                .LeftJoin1(
                context.Users,
                oper => oper.Operator,
                user => user.ID,
                (oper, user) => new
                {
                    oper.PatronLibraryCode,
                    UserLibraryCode = user == null ? null : user.LibraryCodeList,
                    oper.Action,
                    oper.Operator,
                    oper.OperTime,
                    oper.XmlRecPath,
                    oper.Size,
                    oper.ObjectID,
                }
                )
                .Where(x => (x.UserLibraryCode != null && (libraryCode == "*" || x.UserLibraryCode.IndexOf(libraryCode) != -1))
                || (x.PatronLibraryCode != null && (libraryCode0 == "*" || x.PatronLibraryCode == libraryCode0)))
            .LeftJoin1(
                context.Biblios,
                oper => oper.XmlRecPath,
                biblio => biblio.RecPath,
                (oper, biblio) => new
                {
                    Operation = oper.Action,
                    BiblioRecPath = biblio == null ? null : biblio.RecPath,
                    Summary = biblio.Summary,
                    ObjectSize = oper.Size,
                    oper.ObjectID,
                    OperTime = oper.OperTime,
                    Operator = oper.Operator
                }
            )
            .OrderBy(x => x.OperTime)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 入馆登记工作量，按门名称
        // parameters:
        public static int BuildReport_482(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;

            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            var items = context.PassGateOpers
                .Where(b => b.Operation == "passgate"
                && (libraryCode == "*" || b.LibraryCode.Contains(libraryCode))
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
                .Select(oper => new
                {
                    oper.GateName,
                    PassCount = oper.Action == "" ? 1 : 0,
                    TotalCount = 1,
                })
            .GroupBy(x => x.GateName)
            .Select(g => new
            {
                GateName = g.Key,
                PassCount = g.Sum(x => x.PassCount),
                TotalCount = g.Sum(x => x.TotalCount),
            })
            .OrderBy(x => x.GateName)
            .ToList();

            /*
            var items = context.PassGateOpers
                .Where(b => b.Operation == "passgate"
    && (libraryCode == "*" || b.LibraryCode.IndexOf(libraryCode) != -1)
&& string.Compare(b.Date, strStartDate) >= 0
&& string.Compare(b.Date, strEndDate) <= 0)
                .GroupBy(x => x.GateName)
                .Select(g => new
                {
                    GateName = g.Key,
                    PassCount = g.Count(x => x.Action == "passgate"),
                    TotalCount = g.Count(),
                })
                .OrderBy(x => x.GateName)
                .ToList();
                */

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }


        // 入馆登记流水
        // 用读者记录的馆代码来进行分馆筛选; 或者用日志记录的馆代码来筛选?
        // parameters:
        public static int BuildReport_481(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;

            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            var items = context.PassGateOpers
                .Where(b => b.Operation == "passgate"
                && (libraryCode == "*" || b.LibraryCode.Contains(libraryCode))
                && string.Compare(b.Date, strStartDate) >= 0
                && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
                context.Patrons,
                oper => oper.ReaderBarcode,
                patron => patron.Barcode,
                (oper, patron) => new
                {
                    GateName = oper.GateName,
                    PatronBarcode = oper.ReaderBarcode,
                    PatronName = patron.Name,
                    patron.Department,
                    Operation = oper.Action,
                    oper.OperTime,
                    oper.Operator,
                }
                )
            .OrderBy(x => x.OperTime)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }


        // 违约金工作量，按操作者
        // parameters:
        public static int BuildReport_472(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;

            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            var items = context.AmerceOpers
                .Where(b => b.Operation == "amerce"
                && (libraryCode == "*" || b.LibraryCode.Contains(libraryCode))
                && string.Compare(b.Date, strStartDate) >= 0
                && string.Compare(b.Date, strEndDate) <= 0)
                .Select(oper => new
                {
                    oper.Operator,
                    AmerceCount = oper.Action == "amerce" ? 1 : 0,
                    AmerceMoney = oper.Action == "amerce" ? oper.Unit + oper.Price.ToString() : "",

                    ModifyCount = oper.Action == "modifyprice" ? 1 : 0,
                    ModifyMoney = oper.Action == "modifyprice" ? oper.Unit + oper.Price.ToString() : "",

                    UndoCount = oper.Action == "undo" ? 1 : 0,
                    UndoMoney = oper.Action == "undo" ? oper.Unit + oper.Price.ToString() : "",

                    ExpireCount = oper.Action == "expire" ? 1 : 0,

                    TotalCount = 1,
                }
                )
                .AsEnumerable()
            .GroupBy(x => x.Operator)
            .Select(g => new
            {
                Operator = g.Key,
                AmerceCount = g.Sum(x => x.AmerceCount),
                AmerceMoney = g.SumPrice(x => x.AmerceMoney),
                ModifyCount = g.Sum(x => x.ModifyCount),
                ModifyMoney = g.SumPrice(x => x.ModifyMoney),
                UndoCount = g.Sum(x => x.UndoCount),
                UndoMoney = g.SumPrice(x => x.UndoMoney),
                ExpireCount = g.Sum(x => x.ExpireCount),
                TotalCount = g.Sum(x => x.TotalCount),
            })
            .Select(x => new
            {
                x.Operator,
                x.AmerceCount,
                x.AmerceMoney,
                x.ModifyCount,
                x.ModifyMoney,
                x.UndoCount,
                x.UndoMoney,
                x.ExpireCount,
                x.TotalCount,
                FinalAmerceMoney = Substract(x.AmerceMoney, x.UndoMoney),
            })
            .OrderBy(x => x.Operator)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        public static string Substract(string p1, string p2)
        {
            var price = new PriceUtil();
            price.Set(p1).Substract(p2);
            return price.ToString();
        }

        public static string Divide(string p1, string p2)
        {
            var price = new PriceUtil();
            price.Set(p1).Divide(p2);
            return price.ToString();
        }

        /*
        public static string SumPrice(this IEnumerable<string> collection)
        {
            return collection.Aggregate((a, b) => PriceUtil.Add(a, b));
        }
        */

        // 违约金流水
        // 用读者记录的馆代码来进行分馆筛选; 或者用日志记录的馆代码来筛选?
        // parameters:
        public static int BuildReport_471(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;

            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            /*
            DateTime start_time = DateTimeUtil.Long8ToDateTime(strStartDate);

            // TODO: 12 点
            DateTime end_time = DateTimeUtil.Long8ToDateTime(strEndDate);
            */

            var items = context.AmerceOpers
                .Where(b => b.Operation == "amerce"
                && (libraryCode == "*" || b.LibraryCode.Contains(libraryCode))
                && string.Compare(b.Date, strStartDate) >= 0
                && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
                context.Patrons,
                oper => oper.ReaderBarcode,
                patron => patron.Barcode,
                (oper, patron) => new
                {
                    oper.AmerceRecPath,
                    oper.Reason,
                    oper.Price,
                    oper.Unit,
                    oper.ReaderBarcode,
                    PatronName = patron.Name,
                    patron.Department,
                    oper.ItemBarcode,
                    Operation = oper.Action,
                    oper.OperTime,
                    oper.Operator,
                }
                )
                .LeftJoin1(context.Items,
                oper => oper.ItemBarcode,
                item => item.ItemBarcode,
                (oper, item) => new
                {
                    oper.AmerceRecPath,
                    oper.Reason,
                    oper.Price,
                    oper.Unit,
                    oper.ReaderBarcode,
                    oper.PatronName,
                    oper.Department,
                    oper.ItemBarcode,
                    item.BiblioRecPath,
                    oper.Operation,
                    oper.OperTime,
                    oper.Operator
                })
            .LeftJoin1(
                context.Biblios,
                oper => oper.BiblioRecPath,
                biblio => biblio.RecPath,
                (oper, biblio) => new
                {
                    oper.AmerceRecPath,
                    oper.Reason,
                    Price = oper.Price,
                    oper.Unit,
                    PatronBarcode = oper.ReaderBarcode,
                    oper.PatronName,
                    oper.Department,
                    oper.ItemBarcode,
                    biblio.Summary,
                    oper.Operation,
                    oper.OperTime,
                    oper.Operator
                }
            )
            .OrderBy(x => x.OperTime)
            /*
            .AsEnumerable()
            .Select(item => new
            {
                item.AmerceRecPath,
                item.Reason,
                Price = item.Price,
                item.Unit,
                item.PatronBarcode,
                item.PatronName,
                item.Department,
                item.ItemBarcode,
                item.Summary,
                item.Operation,
                item.OperTime,
                item.Operator
            }) */
            .ToList();

            // TODO: 进行 sum?

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 出纳工作量，按馆藏地点
        // parameters:
        public static int BuildReport_443(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;

            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            var items = context.CircuOpers
                .Where(b => (b.Operation == "borrow" || b.Operation == "return")
                && (libraryCode == "*" || b.LibraryCode.Contains(libraryCode))
                && string.Compare(b.Date, strStartDate) >= 0
                && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(context.Items,
                oper => oper.ItemBarcode,
                item => item.ItemBarcode,
                (oper, item) => new
                {
                    item.Location,
                    BorrowCount = oper.Action == "borrow" ? 1 : 0,
                    RenewCount = oper.Action == "renew" ? 1 : 0,
                    ReturnCount = oper.Action == "return" ? 1 : 0,
                    LostCount = oper.Action == "lost" ? 1 : 0,
                    ReadCount = oper.Action == "read" ? 1 : 0,
                    TotalCount = 1,
                })
            .GroupBy(x => x.Location)
            .Select(g => new
            {
                Location = g.Key,
                BorrowCount = g.Sum(x => x.BorrowCount),
                RenewCount = g.Sum(x => x.RenewCount),
                ReturnCount = g.Sum(x => x.ReturnCount),
                LostCount = g.Sum(x => x.LostCount),
                ReadCount = g.Sum(x => x.ReadCount),
                TotalCount = g.Sum(x => x.TotalCount),
            })
            .OrderBy(x => x.Location)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }


        // 出纳工作量，按操作者
        // parameters:
        public static int BuildReport_442(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;
            
            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            var items = context.CircuOpers
                .Where(b => (b.Operation == "borrow" || b.Operation == "return")
                && (libraryCode == "*" || b.LibraryCode.Contains(libraryCode))
                && string.Compare(b.Date, strStartDate) >= 0
                && string.Compare(b.Date, strEndDate) <= 0)
                .Select(oper => new
                {
                    oper.Operator,
                    BorrowCount = oper.Action == "borrow" ? 1 : 0,
                    RenewCount = oper.Action == "renew" ? 1 : 0,
                    ReturnCount = oper.Action == "return" ? 1 : 0,
                    LostCount = oper.Action == "lost" ? 1 : 0,
                    ReadCount = oper.Action == "read" ? 1 : 0,
                    TotalCount = 1,
                }
                )
            .GroupBy(x => x.Operator)
            .Select(g => new
            {
                Operator = g.Key,
                BorrowCount = g.Sum(x => x.BorrowCount),
                RenewCount = g.Sum(x => x.RenewCount),
                ReturnCount = g.Sum(x => x.ReturnCount),
                LostCount = g.Sum(x => x.LostCount),
                ReadCount = g.Sum(x => x.ReadCount),
                TotalCount = g.Sum(x => x.TotalCount),
            })
            .OrderBy(x => x.Operator)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }


        // 出纳流水
        // 用读者记录的馆代码来进行分馆筛选; 或者用日志记录的馆代码来筛选?
        // parameters:
        public static int BuildReport_441(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");
            macro_table["%library%"] = libraryCode;

            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            DateTime start_time = DateTimeUtil.Long8ToDateTime(strStartDate);

            // TODO: 12 点
            DateTime end_time = DateTimeUtil.Long8ToDateTime(strEndDate);

            var items = context.CircuOpers
                .Where(b => (b.Operation == "borrow" || b.Operation == "return")
                && (libraryCode == "*" || b.LibraryCode.Contains(libraryCode))
                && string.Compare(b.Date, strStartDate) >= 0
                && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
                context.Patrons,
                oper => oper.ReaderBarcode,
                patron => patron.Barcode,
                (oper, patron) => new
                {
                    oper.ReaderBarcode,
                    PatronName = patron.Name,
                    patron.Department,
                    oper.ItemBarcode,
                    Operation = oper.Action,
                    oper.OperTime,
                    oper.Operator,
                }
                )
                .LeftJoin1(context.Items,
                oper => oper.ItemBarcode,
                item => item.ItemBarcode,
                (oper, item) => new
                {
                    oper.ReaderBarcode,
                    oper.PatronName,
                    oper.Department,
                    oper.ItemBarcode,
                    item.BiblioRecPath,
                    oper.Operation,
                    oper.OperTime,
                    oper.Operator
                })
            .LeftJoin1(
                context.Biblios,
                oper => oper.BiblioRecPath,
                biblio => biblio.RecPath,
                (oper, biblio) => new
                {
                    PatronBarcode = oper.ReaderBarcode,
                    oper.PatronName,
                    oper.Department,
                    oper.ItemBarcode,
                    biblio.Summary,
                    oper.Operation,
                    oper.OperTime,
                    oper.Operator
                }
            )
            .OrderBy(x => x.OperTime)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 册登记工作量
        // 和 BuildReport_412() 的差异是多输出了一个 transfer(转移) 列
        // parameters:
        public static int BuildReport_432(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;

            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            var items = context.ItemOpers
                .Where(b => b.Operation == "setEntity"
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
                context.Users,
                oper => oper.Operator,
                user => user.ID,
                (oper, user) => new
                {
                    UserLibraryCode = user == null ? "" : user.LibraryCodeList,
                    oper.Action,
                    oper.Operator,
                    NewCount = oper.Action == "new" ? 1 : 0,
                    ChangeCount = oper.Action == "change" ? 1 : 0,
                    DeleteCount = oper.Action == "delete" ? 1 : 0,
                    CopyCount = oper.Action == "copy" ? 1 : 0,
                    MoveCount = oper.Action == "move" ? 1 : 0,
                    TransferCount = oper.Action == "transfer" ? 1 : 0,
                    TotalCount = 1,
                }
                )
                .Where(x => libraryCode == "*" || x.UserLibraryCode.IndexOf(libraryCode) != -1)
            .GroupBy(x => x.Operator)
            .Select(g => new
            {
                Operator = g.Key,
                NewCount = g.Sum(x => x.NewCount),
                ChangeCount = g.Sum(x => x.ChangeCount),
                DeleteCount = g.Sum(x => x.DeleteCount),
                CopyCount = g.Sum(x => x.CopyCount),
                MoveCount = g.Sum(x => x.MoveCount),
                TransferCount = g.Sum(x => x.TransferCount),
                TotalCount = g.Sum(x => x.TotalCount),
            })
            .OrderBy(x => x.Operator)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 册登记流水
        // 和 BuildReport_411() 的区别是多输出一列 ItemBarcode
        // parameters:
        public static int BuildReport_431(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;

            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            DateTime start_time = DateTimeUtil.Long8ToDateTime(strStartDate);

            // TODO: 12 点
            DateTime end_time = DateTimeUtil.Long8ToDateTime(strEndDate);

            // 权且用操作者的所属馆代码来匹配 libraryCode
            var items = context.ItemOpers
                .Where(b => b.Operation == "setEntity"
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
                context.Users,
                oper => oper.Operator,
                user => user.ID,
                (oper, user) => new
                {
                    UserLibraryCode = user.LibraryCodeList,
                    oper.BiblioRecPath,
                    oper.ItemRecPath,
                    oper.Operation,
                    oper.Action,
                    oper.OperTime,
                    oper.Operator,
                }
                )
                .Where(x => libraryCode == "*" || x.UserLibraryCode.IndexOf(libraryCode) != -1)
            .LeftJoin1(
                context.Biblios,
                oper => oper.BiblioRecPath,
                biblio => biblio.RecPath,
                (oper, biblio) => new
                {
                    Operation = oper.Operation + "." + oper.Action,
                    oper.ItemRecPath,
                    BiblioRecPath = oper.BiblioRecPath,
                    Summary = biblio.Summary,
                    OperTime = oper.OperTime,
                    Operator = oper.Operator
                }
            )
            .LeftJoin1(
                context.Items,
                oper => oper.ItemRecPath,
                item => item.ItemRecPath,
                (oper, item) => new
                {
                    oper.Operation,
                    oper.ItemRecPath,
                    oper.BiblioRecPath,
                    oper.Summary,
                    oper.OperTime,
                    oper.Operator,
                    item.ItemBarcode,
                }
            ).OrderBy(x => x.OperTime)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 编目工作量
        // parameters:
        public static int BuildReport_422(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;

            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            // TODO: 订购记录怎么看出是哪个分馆的? 1) 从操作者的权限可以看出 2) 从日志记录的 libraryCode 可以看出 3) 从订购记录的 distribute 元素可以看出
            var items = context.BiblioOpers
                .Where(b => b.Operation == "setBiblioInfo"
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
                context.Users,
                oper => oper.Operator,
                user => user.ID,
                (oper, user) => new
                {
                    UserLibraryCode = user == null ? "" : user.LibraryCodeList,
                    oper.Action,
                    oper.Operator,
                    NewCount = oper.Action == "new" ? 1 : 0,
                    ChangeCount = oper.Action == "change" ? 1 : 0,
                    DeleteCount = oper.Action == "delete" ? 1 : 0,
                    CopyCount = oper.Action == "copy" ? 1 : 0,
                    MoveCount = oper.Action == "move" ? 1 : 0,
                    TotalCount = 1,
                }
                )
                .Where(x => libraryCode == "*" || x.UserLibraryCode.IndexOf(libraryCode) != -1)
            .GroupBy(x => x.Operator)
            .Select(g => new
            {
                Operator = g.Key,
                NewCount = g.Sum(x => x.NewCount),
                ChangeCount = g.Sum(x => x.ChangeCount),
                DeleteCount = g.Sum(x => x.DeleteCount),
                CopyCount = g.Sum(x => x.CopyCount),
                MoveCount = g.Sum(x => x.MoveCount),
                TotalCount = g.Sum(x => x.TotalCount),
            })
            .OrderBy(x => x.Operator)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 编目流水
        // parameters:
        public static int BuildReport_421(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;

            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            // 编目操作都是全局的，不属于某个分馆。这里权且按照工作人员所属的分馆来进行统计
            var items = context.BiblioOpers
                .Where(b => b.Operation == "setBiblioInfo"
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
                context.Users,
                oper => oper.Operator,
                user => user.ID,
                (oper, user) => new
                {
                    UserLibraryCode = user.LibraryCodeList,
                    oper.BiblioRecPath,
                    oper.Operation,
                    oper.Action,
                    oper.OperTime,
                    oper.Operator,
                }
                )
                .Where(x => libraryCode == "*" || x.UserLibraryCode.IndexOf(libraryCode) != -1)
            .LeftJoin1(
                context.Biblios,
                oper => oper.BiblioRecPath,
                biblio => biblio.RecPath,
                (oper, biblio) => new
                {
                    Operation = oper.Operation + "." + oper.Action,
                    BiblioRecPath = oper.BiblioRecPath,
                    Summary = biblio.Summary,
                    OperTime = oper.OperTime,
                    Operator = oper.Operator
                }
            )
            .OrderBy(x => x.OperTime)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 订购工作量
        // 本函数也被 452 报表所用
        // parameters:
        //      operation   为 "setEntity" 或 "setOrder" 等
        public static int BuildReport_412(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
string operation,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            if (string.IsNullOrEmpty(operation))
                throw new ArgumentException("operation 参数值不允许为空");

            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");

            macro_table["%library%"] = libraryCode;

            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            // TODO: 订购记录怎么看出是哪个分馆的? 1) 从操作者的权限可以看出 2) 从日志记录的 libraryCode 可以看出 3) 从订购记录的 distribute 元素可以看出
            var items = context.ItemOpers
                .Where(b => b.Operation == operation
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
                context.Users,
                oper => oper.Operator,
                user => user.ID,
                (oper, user) => new
                {
                    UserLibraryCode = user == null ? "" : user.LibraryCodeList,
                    oper.Action,
                    oper.Operator,
                    NewCount = oper.Action == "new" ? 1 : 0,
                    ChangeCount = oper.Action == "change" ? 1 : 0,
                    DeleteCount = oper.Action == "delete" ? 1 : 0,
                    CopyCount = oper.Action == "copy" ? 1 : 0,
                    MoveCount = oper.Action == "move" ? 1 : 0,
                    TotalCount = 1,
                }
                )
                .Where(x => libraryCode == "*" || x.UserLibraryCode.IndexOf(libraryCode) != -1)
            .GroupBy(x => x.Operator)
            .Select(g => new
            {
                Operator = g.Key,
                NewCount = g.Sum(x => x.NewCount),
                ChangeCount = g.Sum(x => x.ChangeCount),
                DeleteCount = g.Sum(x => x.DeleteCount),
                CopyCount = g.Sum(x => x.CopyCount),
                MoveCount = g.Sum(x => x.MoveCount),
                TotalCount = g.Sum(x => x.TotalCount),
            })
            .OrderBy(x => x.Operator)
            .ToList();

            if (items.Count == 0
                && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 是否不创建空内容的 .rml 文件?
        // 如果 param_table 中没有 writeStyle=passEmpty 参数，默认要创建空内容的 .rml 文件
        static bool PassWrite(Hashtable param_table)
        {
            var style = GetParam(param_table, "writeStyle", false);
            if (StringUtil.IsInList("passEmpty", style))
                return true;
            return false;
        }

        // 订购流水
        // 本函数也被 451 报表所用
        // parameters:
        //      operation   为 "setEntity" 或 "setOrder" 等
        public static int BuildReport_411(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
string operation,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            if (string.IsNullOrEmpty(operation))
                throw new ArgumentException("operation 参数值不允许为空");

            // 注: libraryCode 要求是一个馆代码，或者 *
            string libraryCode = GetParam(param_table, "libraryCode");
            
            macro_table["%library%"] = libraryCode;

            //if (libraryCode != "*")
            //    libraryCode = "," + libraryCode + ",";
            libraryCode = GetMatchLibraryCode(libraryCode);

            /*
            DateTime start_time = DateTimeUtil.Long8ToDateTime(strStartDate);

            // TODO: 12 点
            DateTime end_time = DateTimeUtil.Long8ToDateTime(strEndDate);
            */

            /*
            var items = from oper in context.ItemOpers
                        join biblio in context.Biblios on oper.BiblioRecPath equals biblio.RecPath
                        into joined
                        from result in joined.DefaultIfEmpty()
                        select new
                        {
                            Operation = oper.Operation + "." + oper.Action,
                            oper.ItemRecPath,
                            BiblioRecPath = oper.BiblioRecPath,
                            Summary = result == null ? "" : result.Summary,
                            OperTime = oper.OperTime,
                            Operator = oper.Operator
                        };
                        */

            // TODO: 订购记录怎么看出是哪个分馆的? 1) 从操作者的权限可以看出 2) 从日志记录的 libraryCode 可以看出 3) 从订购记录的 distribute 元素可以看出
            var items = context.ItemOpers
                .Where(b => b.Operation == operation
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
                context.Users,
                oper => oper.Operator,
                user => user.ID,
                (oper, user) => new
                {
                    UserLibraryCode = user.LibraryCodeList,
                    oper.BiblioRecPath,
                    oper.ItemRecPath,
                    oper.Operation,
                    oper.Action,
                    oper.OperTime,
                    oper.Operator,
                }
                )
                .Where(x => libraryCode == "*" || x.UserLibraryCode.IndexOf(libraryCode) != -1)
            .LeftJoin1(
                context.Biblios,
                oper => oper.BiblioRecPath,
                biblio => biblio.RecPath,
                (oper, biblio) => new
                {
                    Operation = oper.Operation + "." + oper.Action,
                    oper.ItemRecPath,
                    BiblioRecPath = oper.BiblioRecPath,
                    Summary = biblio.Summary,
                    OperTime = oper.OperTime,
                    Operator = oper.Operator
                }
            )
            .OrderBy(x => x.OperTime)
            .ToList();

            /*
            var items = context.ItemOpers
                .SelectMany(a => context.Biblios
.Where(b => b.RecPath == a.BiblioRecPath
&& a.Operation == "setEntity"
&& string.Compare(a.Date, strStartDate) >= 0
&& string.Compare(a.Date, strEndDate) <= 0)
.DefaultIfEmpty()
.Select(b => new {
    Operation = a.Action,
    ItemRecPath = a.ItemRecPath,
    BiblioRecPath = a.BiblioRecPath,
    Summary = b == null ? "" : b.Summary,
    a.OperTime,
    a.Operator
})
                ).ToList();
                */

            if (items.Count == 0
                && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 图书在架情况
        // 注：只有 strEndDate 有效。缺点是只能在 strEndDate 当天统计册记录中的 borrower 才准确
        // 302.xml 中通过配置 <property fresh="true" />，令本报表只能是在每一次启动每日统计的当时新鲜时段内容才会统计，其它时段不会统计，这样保证了统计结果的准确性
        public static int BuildReport_302(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            string location = GetParam(param_table, "location");
            string classType = GetParam(param_table, "classType");

            // 注: 如果 location 为 "/" 或者 "望湖小学/"，表示希望前方一致匹配数据字段内容
            bool left_match = false;
            if (location != null && location.EndsWith("/"))
                left_match = true;

            macro_table["%location%"] = location;
            macro_table["%class%"] = classType;

            // TODO: 12 点
            DateTime end_time = DateTimeUtil.Long8ToDateTime(strEndDate);

            var items = context.Items
            .Where(b => left_match ? b.Location.StartsWith(location) : b.Location == location
            // && b.CreateTime >= start_time
            && b.CreateTime <= end_time)
            .LeftJoin1(
                context.Keys,
                item => new { recpath = item.BiblioRecPath, type = classType, index = 0 },
                key => new { recpath = key.BiblioRecPath, type = key.Type, index = key.Index },
                (item, key) => new
                {
                    // item.ItemBarcode,
                    item.Location,
                    // ClassType = key.Type,
                    BiblioRecPath = item.BiblioRecPath,
                    ItemCount = 1,
                    InnerCount = string.IsNullOrEmpty(item.Borrower) ? 1 : 0,
                    OuterCount = string.IsNullOrEmpty(item.Borrower) ? 0 : 1,
                    Class = string.IsNullOrEmpty(key.Text) ? "" : key.Text.Substring(0, 1),
                }
            )
            // .DefaultIfEmpty()
            .Where(x => left_match ? x.Location.StartsWith(location) : x.Location == location
            // && x.ClassType == classType
            )
            .AsEnumerable()
            .GroupBy(x => x.Class)
            .Select(g => new
            {
                Class = g.Key,
                ItemCount = g.Sum(x => x.ItemCount),
                InnerCount = g.Sum(x => x.InnerCount),
                OuterCount = g.Sum(x => x.OuterCount),
            })
            .Select(x => new
            {
                x.Class,
                x.ItemCount,
                x.InnerCount,
                x.OuterCount,
                Percent = String.Format("{0,3:N}%", ((double)x.OuterCount / (double)x.ItemCount) * (double)100)
            })
            .OrderBy(x => x.Class)
            .ToList();

            // https://damieng.com/blog/2014/09/04/optimizing-sum-count-min-max-and-average-with-linq
            // sum line
            var sums = items.GroupBy(g => 1)
                .Select(g => new
                {
                    Class = "",
                    ItemCount = g.Sum(x => x.ItemCount),
                    InnerCount = g.Sum(x => x.InnerCount),
                    OuterCount = g.Sum(x => x.OuterCount),
                })
            .Select(x => new
            {
                x.Class,
                x.ItemCount,
                x.InnerCount,
                x.OuterCount,
                Percent = String.Format("{0,3:N}%", ((double)x.OuterCount / (double)x.ItemCount) * (double)100)
            })
            .ToList();

            Debug.Assert(sums.Count == 1);

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            sums[0],
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 图书种册, 按分类，馆藏地
        public static int BuildReport_301(LibraryContext context,
    Hashtable param_table,
    string strStartDate,
    string strEndDate,
    ReportWriter writer,
    Hashtable macro_table,
    string strOutputFileName)
        {
            string location = param_table["location"] as string;
            string classType = param_table["classType"] as string;

            if (string.IsNullOrEmpty(location))
                throw new ArgumentException("尚未指定 param_table 中的 location 参数");

            if (string.IsNullOrEmpty(classType))
                throw new ArgumentException("尚未指定 param_table 中的 classType 参数");

            // 注: 如果 location 为 "/" 或者 "望湖小学/"，表示希望前方一致匹配数据字段内容
            bool left_match = false;
            if (location != null && location.EndsWith("/"))
                left_match = true;

            macro_table["%location%"] = location;
            macro_table["%class%"] = classType;

            // TODO: 0 点
            DateTime start_time = DateTimeUtil.Long8ToDateTime(strStartDate);
            // TODO: 12 点
            DateTime end_time = DateTimeUtil.Long8ToDateTime(strEndDate);

            var items = context.Items
            .Where(b => left_match ? b.Location.StartsWith(location) : b.Location == location
            // && b.CreateTime >= start_time
            && b.CreateTime <= end_time)
            .LeftJoin1(
                context.Keys,
                item => new { recpath = item.BiblioRecPath, type = classType, index = 0 },
                key => new { recpath = key.BiblioRecPath, type = key.Type, index = key.Index },
                (item, key) => new
                {
                    // item.ItemBarcode,
                    item.Location,
                    // ClassType = key.Type,
                    BiblioRecPath = item.BiblioRecPath,
                    ItemCount = 1,
                    NewItemCount = item.CreateTime >= start_time ? 1 : 0,
                    NewBiblioRecPath = item.CreateTime >= start_time ? item.BiblioRecPath : null,
                    Class = string.IsNullOrEmpty(key.Text) ? "" : key.Text.Substring(0, 1),
                }
            )
            // .DefaultIfEmpty()
            .Where(x => left_match ? x.Location.StartsWith(location) : x.Location == location
            // && x.ClassType == classType
            )
            .AsEnumerable()
            .GroupBy(x => x.Class)
            .Select(g => new
            {
                Class = g.Key,
                StartItemCount = g.Sum(x => x.ItemCount),
                StartBiblioCount = g.GroupBy(x => x.BiblioRecPath).Count(),
                DeltaItemCount = g.Sum(x => x.NewItemCount),
                DeltaBiblioCount = g.GroupBy(x => x.NewBiblioRecPath)
                .Where(x => x.Key != null).Count(),
            })
            .OrderBy(x => x.Class)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }


        // 某段时间内、某馆藏地内按照分类的借阅排行
        // 表格行按照 BorrowCount 从大到小排列
        // parameters:
        //      param_table 要求 location/classType 参数。
        //                  location 表示一个馆藏地，例如 "/阅览室"。注意使用新版的正规形态，其中必须包含一个斜杠
        public static int BuildReport_212(LibraryContext context,
            Hashtable param_table,
            string strStartDate,
            string strEndDate,
            ReportWriter writer,
            Hashtable macro_table,
            string strOutputFileName)
        {
            string location = GetParam(param_table, "location");

            // 注: 如果 location 为 "/" 或者 "望湖小学/"，表示希望前方一致匹配数据字段内容
            bool left_match = false;
            if (location != null && location.EndsWith("/"))
                left_match = true;

            string classType = GetParam(param_table, "classType");

            macro_table["%location%"] = location;
            macro_table["%class%"] = classType;

            var items = context.CircuOpers
            .Where(b => // (b.LibraryCode == librarycode) &&
            b.Action == "borrow" || b.Action == "return"
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
            .LeftJoin1(
                context.Items,
                oper => oper.ItemBarcode,
                item => item.ItemBarcode,
                (oper, item) => new
                {
                    // item.ItemBarcode,
                    item.Location,
                    BiblioRecPath = item.BiblioRecPath,
                    BorrowCount = oper.Action == "borrow" ? 1 : 0,
                    ReturnCount = oper.Action == "return" ? 1 : 0,
                    Class = context.Keys
                .Where(x => x.BiblioRecPath == item.BiblioRecPath && x.Type == classType && x.Index == 0)
                .Select(x => string.IsNullOrEmpty(x.Text) ? "" : x.Text.Substring(0, 1)).FirstOrDefault()
                }
            )
            // .DefaultIfEmpty()
            .Where(x => left_match ? x.Location.StartsWith(location) : x.Location == location)
            .GroupBy(x => x.Class)
            .Select(g => new
            {
                Class = g.Key,  // string.IsNullOrEmpty(g.Key) ? "" : g.Key.Substring(0, 1),
                BorrowCount = g.Sum(x => x.BorrowCount),
                ReturnCount = g.Sum(x => x.ReturnCount)
            })
            .OrderByDescending(x => x.BorrowCount).ThenBy(x => x.Class)
            // .OrderBy(x => x.Class)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        public class Item202
        {
            public string? BiblioRecPath { get; set; }
            public string? Summary { get; set; }
            public int ItemCount { get; set; }

        }


        // 某段时间内没有被借出过的图书。这里有个疑问，就是这一段时间以前借了但在这一段时间内来不及还的算不算借过？
        // parameters:
        //      param_table 要求 location 参数。表示一个馆藏地，例如 "/阅览室"。注意使用新版的正规形态，其中必须包含一个斜杠
        public static IList<Item202> Query_202(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
Hashtable macro_table)
        {
            string location = param_table["location"] as string;

            if (string.IsNullOrEmpty(location))
                throw new ArgumentException("尚未指定 param_table 中的 location 参数");

            var items = context.CircuOpers
            .Where(b => // (b.LibraryCode == librarycode) &&
            b.Action == "borrow"
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
            .LeftJoin1(
                context.Items,
                oper => oper.ItemBarcode,
                item => item.ItemBarcode,
                (oper, item) => new
                {
                    item.ItemBarcode,
                    Location = item.Location,
                }
            )
            // .DefaultIfEmpty()
            .Where(x => location == "*" || x.Location == location)
            .Select(x => x.ItemBarcode);    // .ToList();

            var results = context.Items
    .Where(x => x.Location == location && !items.Contains(x.ItemBarcode))
    .GroupBy(x => x.BiblioRecPath)
    .Select(g => new Item202
    {
        BiblioRecPath = g.Key,
        ItemCount = g.Count(),
        Summary = context.Biblios.Where(o => o.RecPath == g.Key).Select(o => o.Summary).FirstOrDefault()
    })
.OrderBy(t => t.BiblioRecPath)
.ToList();

            macro_table["%location%"] = GetLocationCaption(location);
            return results;
        }

        // 某段时间内没有被借出过的图书。这里有个疑问，就是这一段时间以前借了(处于在借状态)但在这一段时间内来不及还的算不算借过？
        // 目前某段时间内处于在借状态的图书，在本报表中处于“未被借阅”范围，这是不太合理的
        // parameters:
        //      param_table 要求 location/dataRange 参数。表示一个馆藏地，例如 "/阅览室"。注意使用新版的正规形态，其中必须包含一个斜杠
        public static int BuildReport_202(LibraryContext context,
            Hashtable param_table,
            string strStartDate,
            string strEndDate,
            ReportWriter writer,
            Hashtable macro_table,
            string strOutputFileName)
        {
            string location = param_table["location"] as string;
            // string librarycode = GetLibraryCode(location);

            if (string.IsNullOrEmpty(location))
                throw new ArgumentException("尚未指定 param_table 中的 location 参数");

            // 注: 如果 location 为 "/" 或者 "望湖小学/"，表示希望前方一致匹配数据字段内容
            bool left_match = false;
            if (location != null && location.EndsWith("/"))
                left_match = true;

            macro_table["%location%"] = location;

            var items = context.CircuOpers
            .Where(b => // (b.LibraryCode == librarycode) &&
            b.Action == "borrow"
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
            .LeftJoin1(
                context.Items,
                oper => oper.ItemBarcode,
                item => item.ItemBarcode,
                (oper, item) => new
                {
                    item.ItemBarcode,
                    Location = item.Location,
                }
            )
            // .DefaultIfEmpty()
            .Where(x => left_match ? x.Location.StartsWith(location) : x.Location == location)
            .Select(x => x.ItemBarcode).ToList();

            /*
            var results = context.Items
                .ToList()
                .Where(x => x.Location == location && !items.Contains(x.ItemBarcode))
                .GroupBy(x => x.BiblioRecPath)
                .Select(g => new
                {
                    BiblioRecPath = g.Key,
                    ItemCount = g.Count(),
                    BarcodeList = g.Select(x => x.ItemBarcode).ToArray()
                })
                .Join(context.Biblios,
            item => item.BiblioRecPath,
            biblio => biblio.RecPath,
            (item, biblio) => new
            {
                BiblioRecPath = item.BiblioRecPath,
                Summary = biblio.Summary,
                ItemCount = item.ItemCount,
                Barcodes = string.Join(",", item.BarcodeList),
            })
            .OrderBy(t => t.BiblioRecPath)
            .ToList();

                  <column name="册条码号列表" align="left" sum="no" class="Barcodes" eval="" />

            */

            var results = context.Items
    .Where(x => (left_match ? x.Location.StartsWith(location) : x.Location == location) && !items.Contains(x.ItemBarcode))
    .GroupBy(x => x.BiblioRecPath)
    .Select(g => new
    {
        BiblioRecPath = g.Key,
        ItemCount = g.Count(),
    })
    .LeftJoin1(context.Biblios,
item => item.BiblioRecPath,
biblio => biblio.RecPath,
(item, biblio) => new
{
    BiblioRecPath = item.BiblioRecPath,
    Summary = biblio.Summary,
    ItemCount = item.ItemCount,
})
.OrderBy(t => t.BiblioRecPath)
.ToList();

            macro_table["%location%"] = GetLocationCaption(location);

            if (results.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            results,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        public class Item201
        {
            public string? RecPath { get; set; }
            public string? Summary { get; set; }
            public int BorrowCount { get; set; }
            public int ReturnCount { get; set; }
        }

        // 按图书种的借阅排行榜
        // parameters:
        //      param_table 要求 location 参数。表示一个馆藏地，例如 "/阅览室"。注意使用新版的正规形态，其中必须包含一个斜杠
        public static IList<Item201> Query_201(LibraryContext context,
        Hashtable param_table,
        string strStartDate,
        string strEndDate,
        // ReportWriter writer,
        Hashtable macro_table)
        {
            string location = param_table["location"] as string;
            // string librarycode = GetLibraryCode(location);

            if (string.IsNullOrEmpty(location))
                throw new ArgumentException("尚未指定 param_table 中的 location 参数");

            // 注: 如果 location 为 "/" 或者 "望湖小学/"，表示希望前方一致匹配数据字段内容
            bool left_match = false;
            if (location != null && location.EndsWith("/"))
                left_match = true;


#if REMOVED
            // 第一步，获得册统计结果
            var items = from oper in context.CircuOpers
                        where (
            (oper.Action == "borrow" || oper.Action == "return")
            && string.Compare(oper.Date, strStartDate) >= 0
            && string.Compare(oper.Date, strEndDate) <= 0)
                        join item in context.Items
                        on oper.ItemBarcode equals item.ItemBarcode
                        into re
                        from r in re.DefaultIfEmpty(null)
                        where r.Location == location
                        select new
                        {
                            Location = r.Location,
                            BiblioRecPath = r.BiblioRecPath,
                            BorrowCount = oper.Action == "borrow" ? 1 : 0,
                            ReturnCount = oper.Action == "return" ? 1 : 0
                        };

            // 第二步，获得书目统计结果
            var biblios = from item in items
                          group item by item.BiblioRecPath
                          into g
                          select new
                          {
                              BiblioRecPath = g.Key,
                              BorrowCount = g.Sum(t => t.BorrowCount),
                              ReturnCount = g.Sum(t => t.ReturnCount)
                          };

            var opers = (from item in biblios
                         join biblio in context.Biblios
                         on item.BiblioRecPath equals biblio.RecPath
                         into re
                         from r in re.DefaultIfEmpty()
                         select new Item201
                         {
                             RecPath = item.BiblioRecPath,
                             Summary = r.Summary,
                             BorrowCount = item.BorrowCount,
                             ReturnCount = item.ReturnCount
                         })
                         .ToList();
#endif

#if REMOVED
            var opers = context.CircuOpers
            .Where(b => // (b.LibraryCode == librarycode) &&
            (b.Action == "borrow" || b.Action == "return")
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
            .LeftJoin(
                context.Items,
                oper => oper.ItemBarcode,
                item => item.ItemBarcode,
                (oper, item) => new
                {
                    Location = item.Location,
                    BiblioRecPath = item.BiblioRecPath,
                    BorrowCount = oper.Action == "borrow" ? 1 : 0,
                    ReturnCount = oper.Action == "return" ? 1 : 0
                }
            )
            .DefaultIfEmpty()
            .Where(x => x.Location == location)
            .GroupBy(x => x.BiblioRecPath)
            .Select(g => new
            {
                BiblioRecPath = g.Key,
                BorrowCount = g.Sum(t => t.BorrowCount),
                ReturnCount = g.Sum(t => t.ReturnCount)
            })
            // .DefaultIfEmpty()
            .LeftJoin(context.Biblios,
            item => item.BiblioRecPath,
            biblio => biblio.RecPath,
            (item, biblio) => new
            Item201
            {
                RecPath = item.BiblioRecPath,
                Summary = biblio.Summary,
                BorrowCount = item.BorrowCount,
                ReturnCount = item.ReturnCount
            })
            .OrderByDescending(t => t.BorrowCount).ThenBy(t => t.RecPath)
            .ToList();
#endif

            var opers = context.CircuOpers
                .Where(b => // (b.LibraryCode == librarycode) &&
(b.Action == "borrow" || b.Action == "return")
&& string.Compare(b.Date, strStartDate) >= 0
&& string.Compare(b.Date, strEndDate) <= 0)
                .LeftJoin1(
    context.Items,
    oper => oper.ItemBarcode,
    item => item.ItemBarcode,
    /*
    oper => new
    {
        Location = (string ?)null,
        BiblioRecPath = (string ?)null,
        BorrowCount = oper.Action == "borrow" ? 1 : 0,
        ReturnCount = oper.Action == "return" ? 1 : 0
    },
    */
    (oper, item) => new
    {
        Location = item.Location,
        BiblioRecPath = item.BiblioRecPath,
        BorrowCount = oper.Action == "borrow" ? 1 : 0,
        ReturnCount = oper.Action == "return" ? 1 : 0
    }
)
.Where(x => left_match ? x.Location.StartsWith(location) : x.Location == location)
.GroupBy(x => x.BiblioRecPath)
.Select(g => new Item201
{
    RecPath = g.Key,
    BorrowCount = g.Sum(t => t.BorrowCount),
    ReturnCount = g.Sum(t => t.ReturnCount),
    Summary = context.Biblios.Where(o => o.RecPath == g.Key).Select(o => o.Summary).FirstOrDefault()
})
// .DefaultIfEmpty()
.OrderByDescending(t => t.BorrowCount).ThenBy(t => t.RecPath)
.ToList();

            /*
            if (opers == null
                || opers.Count == 0)
                return 0;
            */

            macro_table["%location%"] = GetLocationCaption(location);
            return opers;
        }


        // 按图书种的借阅排行榜
        // parameters:
        //      param_table 要求 location 参数。表示一个馆藏地，例如 "/阅览室"。注意使用新版的正规形态，其中必须包含一个斜杠
        public static int BuildReport_201(LibraryContext context,
            Hashtable param_table,
            string strStartDate,
            string strEndDate,
            ReportWriter writer,
            Hashtable macro_table,
            string strOutputFileName)
        {
            string location = param_table["location"] as string;
            // string librarycode = GetLibraryCode(location);

#if REMOVED
            var opers = context.CircuOpers
            .Where(b => // (b.LibraryCode == librarycode) &&
            (b.Action == "borrow" || b.Action == "return")
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
            .LeftJoin(
                context.Items,
                oper => oper.ItemBarcode,
                item => item.ItemBarcode,
                (oper, item) => new
                {
                    Location = item.Location,
                    BiblioRecPath = item.BiblioRecPath,
                    BorrowCount = oper.Action == "borrow" ? 1 : 0,
                    ReturnCount = oper.Action == "return" ? 1 : 0
                }
            )
            .DefaultIfEmpty()
            .Where(x => x.Location == location)
            .GroupBy(x => x.BiblioRecPath)
            .Select(g => new
            {
                BiblioRecPath = g.Key,
                BorrowCount = g.Sum(t => t.BorrowCount),
                ReturnCount = g.Sum(t => t.ReturnCount)
            })
            // .DefaultIfEmpty()
            .LeftJoin(context.Biblios,
            item => item.BiblioRecPath,
            biblio => biblio.RecPath,
            (item, biblio) => new
            {
                RecPath = item.BiblioRecPath,
                Summary = biblio.Summary,
                BorrowCount = item.BorrowCount,
                ReturnCount = item.ReturnCount
            })
            .OrderByDescending(t => t.BorrowCount).ThenBy(t => t.RecPath)
            .ToList();

            /*
            if (opers == null
                || opers.Count == 0)
                return 0;
            */

            macro_table["%location%"] = GetLocationCaption(location);
#endif

            var opers = Query_201(context,
        param_table,
        strStartDate,
            strEndDate,
        macro_table);

            if (opers.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            opers,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 获得适合用作报表名或文件名 的 地点名称字符串
        public static string GetLocationCaption(string strText)
        {
            if (string.IsNullOrEmpty(strText) == true)
                return "[空]";

            if (strText[strText.Length - 1] == '/')
                return strText.Substring(0, strText.Length - 1) + "[全部]";

            return strText;
        }

        // 超期的读者和图书清单
        // parameters:
        //      param_table 要求 libraryCode/endDate 参数
        //                  endDate 统计日期，也就是计算超期的日期，如果为空表示今天
        public static int BuildReport_141(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            DateTime end;

            string date = "";
            date = param_table["endDate"] as string;
            if (string.IsNullOrEmpty(date) == false)
            {
                end = DateTimeUtil.Long8ToDateTime(date);
            }
            else
            {
                date = strEndDate;
                end = DateTimeUtil.Long8ToDateTime(strEndDate);
                DateTime today = DateTime.Now;
                if (end > today)
                    end = today;
            }

            string strLibraryCode = param_table["libraryCode"] as string;

            // 筛选分馆
            if (strLibraryCode != "*")
                strLibraryCode += "/";

            // 从全部册记录里面选择那些超期的
            var items = context.Items
                .Where(x => (strLibraryCode == "*" || x.Location.StartsWith(strLibraryCode))
                && string.IsNullOrEmpty(x.Borrower) == false
                && x.ReturningTime < end)
                .LeftJoin1(context.Patrons,
                item => item.Borrower,
                patron => patron.Barcode,
                (item, patron) => new
                {
                    ItemBarcode = item.ItemBarcode,
                    PatronBarcode = item.Borrower,
                    Period = item.BorrowPeriod,
                    Name = patron == null ? "" : patron.Name,
                    Department = patron.Department,
                    BorrowTime = item.BorrowTime,
                    ReturningTime = item.ReturningTime,
                    Summary = context
                    .Biblios.Where(x => x.RecPath == item.BiblioRecPath)
                    .Select(a => a.Summary)
                    .FirstOrDefault()
                })
            .OrderBy(t => t.BorrowTime)
            .ToList();

            if (items.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            items,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

#if NO
        // 超期的读者和图书清单
        // parameters:
        //      parameters  附加的参数。统计日期，也就是计算超期的日期，如果为空表示今天
        public static int BuildReport_141(LibraryContext context,
string strLibraryCode,
string strStartDate,
string strEndDate,
string[] parameters,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            DateTime end = DateTimeUtil.Long8ToDateTime(strEndDate);

            var opers = context.CircuOpers
            .Where(b => b.Action == "borrow")
            .Select(a => new
            {
                a.ItemBarcode,
                a.ReaderBarcode,
                BorrowTime = a.OperTime,
                a.ReturningTime,
                ReturnTime = context
                    .CircuOpers.Where(x => x.ReaderBarcode == a.ReaderBarcode
                    && x.Action == "return"
                    && x.OperTime >= a.OperTime)
                    .Select(x => new { x.OperTime })
                    .OrderBy(x => x.OperTime)
                    .FirstOrDefault().OperTime
            })
            .Where(x => x.ReturnTime == null && x.ReturningTime < end)
            .Join(
                context.Items,
                oper => oper.ItemBarcode,
                item => item.ItemBarcode,
                (oper, item) => new
                {
                    item.ItemBarcode,
                    PatronBarcode = oper.ReaderBarcode,
                    oper.BorrowTime,
                    oper.ReturnTime,
                    oper.ReturningTime,
                    Summary = context
                    .Biblios.Where(x => x.RecPath == item.BiblioRecPath)
                    .Select(a => a.Summary)
                    .FirstOrDefault()
                }
            )
            .Join(
                context.Patrons,
                item => item.PatronBarcode,
                patron => patron.Barcode,
                (item, patron) => new {
                    item.PatronBarcode,
                    item.ItemBarcode,
                    item.BorrowTime,
                    item.ReturningTime,
                    item.Period,
                    patron.Name,
                    patron.Department,
                }
                )
            .DefaultIfEmpty()
            .OrderBy(t => t.BorrowTime)
            .ToList();

            int nRet = writer.OutputRmlReport(
            opers,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
        }

#endif

        // 单个读者的借阅清单
        // parameters:
        //      param_table 要求 patronBarcode 参数
        //                  patronBarcode 读者证条码号
        public static int BuildReport_131(LibraryContext context,
Hashtable param_table,
string strStartDate,
string strEndDate,
ReportWriter writer,
Hashtable macro_table,
string strOutputFileName)
        {
            string patronBarcode = param_table["patronBarcode"] as string;

            if (string.IsNullOrEmpty(patronBarcode))
                throw new ArgumentException("尚未指定 param_table 中的 patronBarcode 参数");

            macro_table["%readerbarcode%"] = patronBarcode;

            var opers = context.CircuOpers
            .Where(b => (b.ReaderBarcode == patronBarcode)
            && b.Action == "borrow"
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
            .LeftJoin1(
                context.Items,
                oper => oper.ItemBarcode,
                item => item.ItemBarcode,
                (oper, item) => new
                {
                    ItemBarcode = item == null ? "" : item.ItemBarcode,
                    BorrowTime = oper.OperTime,
                    ReturnTime = context
                    .CircuOpers.Where(x => x.ReaderBarcode == patronBarcode && x.Action == "return" && x.OperTime >= oper.OperTime && string.Compare(x.Date, oper.Date) >= 0)
                    .Select(a => a.OperTime)
                    .OrderBy(a => a)
                    .FirstOrDefault(),
                    Summary = context
                    .Biblios.Where(x => item != null && x.RecPath == item.BiblioRecPath)
                    .Select(a => a.Summary)
                    .FirstOrDefault()
                }
            )
            // .DefaultIfEmpty()
            .OrderBy(t => t.BorrowTime)
            //.Take(1000)
            .ToList();

#if REMOVED
            var temp = context.CircuOpers
.Where(b => (b.ReaderBarcode == patronBarcode)
&& b.Action == "borrow"
&& string.Compare(b.Date, strStartDate) >= 0
&& string.Compare(b.Date, strEndDate) <= 0)
.LeftJoin(
    context.Items,
    oper => oper.ItemBarcode,
    item => item.ItemBarcode,
    (oper, item) => new
    {
        ItemBarcode = item == null ? "" : item.ItemBarcode,
        BorrowTime = oper.OperTime,
        ReturnTime = context
        .CircuOpers.Where(x => x.ReaderBarcode == patronBarcode && x.Action == "return" && x.OperTime >= oper.OperTime && string.Compare(x.Date, oper.Date) >= 0)
        .Select(a => a.OperTime)
        // .OrderBy(a => a.OperTime)
        .FirstOrDefault(),
        Summary = context
        .Biblios.Where(x => item != null && x.RecPath == item.BiblioRecPath)
        .Select(a => a.Summary)
        .FirstOrDefault()
    }
);
            List<object> opers = new List<object>();
            foreach(var o in temp.DefaultIfEmpty())
            {
                opers.Add(o);
            }
#endif

            if (opers == null || opers.Count == 0)
                return 0;

            /*
            .LeftJoin(
                context.Items,
                oper => oper.ItemBarcode,
                item => item.ItemBarcode,
                (oper, item) => new
                {
                    ItemBarcode = item == null ? "" : item.ItemBarcode,
                    BorrowTime = oper.OperTime,
                    ReturnTime = context
                    .CircuOpers.Where(x => x.ReaderBarcode == patronBarcode && x.Action == "return" && x.OperTime >= oper.OperTime)
                    .Select(a => new { a.OperTime })
                    .OrderBy(a => a.OperTime)
                    .FirstOrDefault().OperTime,
                    Summary = context
                    .Biblios.Where(x => item != null && x.RecPath == item.BiblioRecPath)
                    .Select(a => a.Summary)
                    .FirstOrDefault()
                }
            )
            .DefaultIfEmpty()
            .OrderBy(t => t.BorrowTime)
            //.Take(1000)
            .ToList();
            */

            if (macro_table.Contains("%name%") == false
                || macro_table.Contains("%department%") == false)
            {
                // %name% %readerbarcode% %department%
                var patron = context.Patrons.Where(x => x.Barcode == patronBarcode)
                    .Select(x => new { x.Name, x.Department })
                    .FirstOrDefault();
                macro_table["%name%"] = patron == null ? "" : patron.Name;
                macro_table["%department%"] = patron == null ? "" : patron.Department;
            }

            if (opers.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            opers,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }


        // 读者姓名的借阅排行
        // parameters:
        //      param_table 要求 libraryCode 参数
        public static int BuildReport_121(LibraryContext context,
    Hashtable param_table,
    string strStartDate,
    string strEndDate,
    ReportWriter writer,
    Hashtable macro_table,
    string strOutputFileName)
        {
            string strLibraryCode = param_table["libraryCode"] as string;

            strLibraryCode = GetMatchLibraryCode(strLibraryCode);

            // TODO: 如何比较日志记录中的 libraryCode ? 应该用 ,code, 来比较?
            var opers = context.CircuOpers
            .Where(b => (strLibraryCode == "*" || b.LibraryCode.Contains(strLibraryCode))
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
            .LeftJoin1(
                context.Patrons,
                oper => oper.ReaderBarcode,
                patron => patron.Barcode,
                (oper, patron) => new
                {
                    PatronBarcode = patron == null ? "" : patron.Barcode,
                    Name = patron == null ? "" : patron.Name,
                    Department = patron == null ? "" : patron.Department,
                    BorrowCount = oper.Action == "borrow" ? 1 : 0,
                    ReturnCount = oper.Action == "return" ? 1 : 0
                }
            )
            // .DefaultIfEmpty()
            .GroupBy(x => new { x.PatronBarcode, x.Name, x.Department },
            (key, items) => new
            {
                PatronBarcode = key.PatronBarcode,
                key.Name,
                key.Department,
                BorrowCount = items.Sum(t => t.BorrowCount),
                ReturnCount = items.Sum(t => t.ReturnCount)
            }
            )
            .OrderByDescending(t => t.BorrowCount).ThenBy(t => t.Name)
            .ToList();

            if (opers.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            opers,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        // 读者类型的借阅排行
        // parameters:
        //      param_table 要求 libraryCode 参数
        public static int BuildReport_111(LibraryContext context,
    Hashtable param_table,
    string strStartDate,
    string strEndDate,
    ReportWriter writer,
    Hashtable macro_table,
    string strOutputFileName)
        {
            string strLibraryCode = param_table["libraryCode"] as string;

            strLibraryCode = GetMatchLibraryCode(strLibraryCode);

            var opers = context.CircuOpers
            .Where(b => (strLibraryCode == "*" || b.LibraryCode.Contains(strLibraryCode))
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
            .LeftJoin1(
                context.Patrons,
                oper => oper.ReaderBarcode,
                patron => patron.Barcode,
                (oper, patron) => new
                {
                    ReaderType = patron == null ? "" : patron.ReaderType,
                    BorrowCount = oper.Action == "borrow" ? 1 : 0,
                    ReturnCount = oper.Action == "return" ? 1 : 0
                }
            )
            // .DefaultIfEmpty()
            .GroupBy(x => x.ReaderType)
            .Select(g => new
            {
                PatronType = g.Key,
                BorrowCount = g.Sum(t => t.BorrowCount),
                ReturnCount = g.Sum(t => t.ReturnCount)
            })
            // .DefaultIfEmpty()
            .OrderByDescending(t => t.BorrowCount).ThenBy(t => t.PatronType)
            .ToList();

            if (opers.Count == 0
    && PassWrite(param_table))
                return 0;

            int nRet = writer.OutputRmlReport(
            opers,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
            return nRet + 1;
        }

        static string GetMatchLibraryCode(string libraryCode)
        {
            if (libraryCode == null)
                return ",,";
            if (libraryCode == "*")
                return libraryCode;
            return "," + libraryCode + ",";
        }

        static bool QueryLibraryCode(string query, string instance)
        {
            return (query == "*" || instance.IndexOf(query) != -1);
        }

        // 按选定部门的图书借阅排行榜
        // parameters:
        //      param_table 要求 libraryCode 参数
        //                  要求 departments 参数，内容为逗号间隔的部门名字列表
        public static int BuildReport_102(LibraryContext context,
            Hashtable param_table,
            string strStartDate,
            string strEndDate,
            ReportWriter writer,
            Hashtable macro_table,
            string strOutputFileName)
        {
            string strLibraryCode = param_table["libraryCode"] as string;
            string? departments = param_table["departments"] as string;

            if (string.IsNullOrEmpty(departments))
                throw new ArgumentException($"param_table 中应当包含 departments 参数");

            var department_list = departments.Split(",");

            /*
            var test = context.CircuOpers
.Where(b => (strLibraryCode == "*" || b.LibraryCode == strLibraryCode)
&& string.Compare(b.Date, strStartDate) >= 0
&& string.Compare(b.Date, strEndDate) <= 0).ToList();
            */
            strLibraryCode = GetMatchLibraryCode(strLibraryCode);

            var opers = context.CircuOpers
            .Where(b => (strLibraryCode == "*" || b.LibraryCode.Contains(strLibraryCode))
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
            .LeftJoin1(
                context.Patrons,
                oper => oper.ReaderBarcode,
                patron => patron.Barcode,
                (oper, patron) => new
                {
                    Department = patron == null ? "" : (department_list.Contains(patron.Department) ? patron.Department : null),
                    BorrowCount = oper.Action == "borrow" ? 1 : 0,
                    ReturnCount = oper.Action == "return" ? 1 : 0,
                    ReadCount = oper.Action == "read" ? 1 : 0,
                }
            )
            // .DefaultIfEmpty()
            .GroupBy(x => x.Department)
            .Select(g => new
            {
                Department = g.Key,
                BorrowCount = g.Sum(t => t.BorrowCount),
                ReturnCount = g.Sum(t => t.ReturnCount),
                ReadCount = g.Sum(t => t.ReadCount),
            })
            .OrderByDescending(t => t.BorrowCount).ThenBy(t => t.Department)
            .ToList();

            if (opers.Count == 0
    && PassWrite(param_table))
                return 0;

            // return:
            //      -1  出错
            //      其它  返回实际处理的表格内容行数(注意，不包括标题行、合计行等)
            int nRet = writer.OutputRmlReport(
            opers,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);

            return nRet + 1;
        }

        // 获得一个分馆内读者记录的所有单位名称(类型)
        public static IEnumerable<string?> GetAllReaderDepartments(
            LibraryContext context,
            string strLibraryCode)
        {
            return context.Patrons.Where(o => o.LibraryCode == strLibraryCode)
                .Select(o => o.Department).Distinct();
        }

        // 按部门的图书借阅排行榜
        // parameters:
        //      param_table 要求 libraryCode 参数
        public static int BuildReport_101(LibraryContext context,
            Hashtable param_table,
            string strStartDate,
            string strEndDate,
            ReportWriter writer,
            Hashtable macro_table,
            string strOutputFileName)
        {
            string strLibraryCode = param_table["libraryCode"] as string;

            /*
            var test = context.CircuOpers
.Where(b => (strLibraryCode == "*" || b.LibraryCode == strLibraryCode)
&& string.Compare(b.Date, strStartDate) >= 0
&& string.Compare(b.Date, strEndDate) <= 0).ToList();
            */
            strLibraryCode = GetMatchLibraryCode(strLibraryCode);

            var opers = context.CircuOpers
            .Where(b => (strLibraryCode == "*" || b.LibraryCode.Contains(strLibraryCode))
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
            .LeftJoin1(
                context.Patrons,
                oper => oper.ReaderBarcode,
                patron => patron.Barcode,
                (oper, patron) => new
                {
                    Department = patron == null ? "" : patron.Department,
                    BorrowCount = oper.Action == "borrow" ? 1 : 0,
                    ReturnCount = oper.Action == "return" ? 1 : 0,
                    ReadCount = oper.Action == "read" ? 1 : 0,
                }
            )
            // .DefaultIfEmpty()
            .GroupBy(x => x.Department)
            .Select(g => new
            {
                Department = g.Key,
                BorrowCount = g.Sum(t => t.BorrowCount),
                ReturnCount = g.Sum(t => t.ReturnCount),
                ReadCount = g.Sum(t => t.ReadCount),
            })
            .OrderByDescending(t => t.BorrowCount).ThenBy(t => t.Department)
            .ToList();

            if (opers.Count == 0
    && PassWrite(param_table))
                return 0;   // 注: 表示没有创建 .rml 文件

            // return:
            //      -1  出错
            //      其它  返回实际处理的表格内容行数(注意，不包括标题行、合计行等)
            int nRet = writer.OutputRmlReport(
            opers,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);

            return nRet + 1;    // 注: 最小是 1，表示创建了一个内容为空的 .rml 文件
        }

        // 测试创建按部门的图书借阅排行榜
        public static void TestBuildReport0(LibraryContext context,
            string strLibraryCode,
            string strDateRange,
            ReportWriter writer,
            Hashtable macro_table,
            string strOutputFileName)
        {
            // 将日期字符串解析为起止范围日期
            // throw:
            //      Exception
            DateTimeUtil.ParseDateRange(strDateRange,
                out string strStartDate,
                out string strEndDate);
            if (string.IsNullOrEmpty(strEndDate) == true)
                strEndDate = strStartDate;

            strLibraryCode = GetMatchLibraryCode(strLibraryCode);

            /*
            var opers = context.CircuOpers
    .Where(b => string.Compare(b.Date, strStartDate) >= 0 && string.Compare(b.Date, strEndDate) <= 0)
    .ToList();
    */
            var opers = context.CircuOpers
            .Where(b => (strLibraryCode == "*" || b.LibraryCode.Contains(strLibraryCode))
            && string.Compare(b.Date, strStartDate) >= 0
            && string.Compare(b.Date, strEndDate) <= 0)
            .LeftJoin1(
                context.Patrons,
                oper => oper.ReaderBarcode,
                patron => patron.Barcode,
                (oper, patron) => new
                {
                    Department = patron == null ? "" : patron.Department,
                    BorrowCount = oper.Action == "borrow" ? 1 : 0,
                    ReturnCount = oper.Action == "return" ? 1 : 0
                }
            )
            .AsEnumerable()
            .GroupBy(x => x.Department)
            .Select(g => new
            {
                Department = g.FirstOrDefault().Department,
                BorrowCount = g.Sum(t => t.BorrowCount),
                ReturnCount = g.Sum(t => t.ReturnCount)
            })
            .OrderByDescending(t => t.BorrowCount).ThenBy(t => t.Department)
            .ToList();

            macro_table["%daterange%"] = strDateRange;
            macro_table["%library%"] = strLibraryCode;

            int nRet = writer.OutputRmlReport(
            opers,
            null,
            macro_table,
            strOutputFileName,
            out string strError);
            if (nRet == -1)
                throw new Exception(strError);
        }

        /*
        public static bool MatchLibraryCode(string libraryCode, string pattern)
        {
            if (pattern == "*")
                return true;
            return string.Compare(libraryCode, pattern) == 0;
        }
        */

        #region  RmlToExcel

        // RML 格式转换为 Excel 文件
        public static int RmlToExcel(string strRmlFileName,
    string strExcelFileName,
    out string strError)
        {
            strError = "";
            int nRet = 0;

            using (Stream stream = File.OpenRead(strRmlFileName))
            using (XmlTextReader reader = new XmlTextReader(stream))
            {
                while (true)
                {
                    bool bRet = reader.Read();
                    if (bRet == false)
                    {
                        strError = "文件 " + strRmlFileName + " 没有根元素";
                        return -1;
                    }
                    if (reader.NodeType == XmlNodeType.Element)
                        break;
                }

                ExcelDocument doc = ExcelDocument.Create(strExcelFileName);
                try
                {
                    doc.NewSheet("Sheet1");

                    int nColIndex = 0;
                    int _lineIndex = 0;

                    string strTitle = "";
                    string strComment = "";
                    string strCreateTime = "";
                    // string strCss = "";
                    List<ColumnStyle> col_defs = null;

                    while (true)
                    {
                        bool bRet = reader.Read();
                        if (bRet == false)
                            break;
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name == "title")
                            {
                                strTitle = reader.ReadInnerXml();
                            }
                            else if (reader.Name == "comment")
                            {
                                strComment = reader.ReadInnerXml();
                            }
                            else if (reader.Name == "createTime")
                            {
                                strCreateTime = reader.ReadElementContentAsString();
                            }
                            else if (reader.Name == "style")
                            {
                                // strCss = reader.ReadElementContentAsString();
                            }
                            else if (reader.Name == "columns")
                            {
                                // 从 RML 文件中读入 <columns> 元素
                                nRet = ReadColumnStyle(reader,
            out col_defs,
            out strError);
                                if (nRet == -1)
                                {
                                    strError = "ReadColumnStyle() error : " + strError;
                                    return -1;
                                }

                            }
                            else if (reader.Name == "table")
                            {
                                List<string> lines = null;

                                nRet = ParseLines(strTitle,
           out lines,
           out strError);
                                if (nRet == -1)
                                {
                                    strError = "解析 title 内容 '" + strTitle + "' 时发生错误: " + strError;
                                    return -1;
                                }

                                // 输出标题文字
                                nColIndex = 0;
                                foreach (string t in lines)
                                {
                                    List<CellData> cells = new List<CellData>();
                                    cells.Add(new CellData(nColIndex, t));
                                    doc.WriteExcelLine(_lineIndex, cells);
                                    _lineIndex++;
                                }

                                nRet = ParseLines(strComment,
out lines,
out strError);
                                if (nRet == -1)
                                {
                                    strError = "解析 comment 内容 '" + strTitle + "' 时发生错误: " + strError;
                                    return -1;
                                }
                                nColIndex = 0;
                                foreach (string t in lines)
                                {
                                    List<CellData> cells = new List<CellData>();
                                    cells.Add(new CellData(nColIndex, t));
                                    doc.WriteExcelLine(_lineIndex, cells);

                                    _lineIndex++;
                                }

                                // 空行
                                _lineIndex++;

                            }
                            else if (reader.Name == "tr")
                            {
                                // 输出一行
                                List<CellData> cells = null;
                                nRet = ReadLine(reader,
                                    col_defs,
            out cells,
            out strError);
                                if (nRet == -1)
                                {
                                    strError = "ReadLine error : " + strError;
                                    return -1;
                                }
                                doc.WriteExcelLine(_lineIndex, cells, WriteExcelLineStyle.None);
                                _lineIndex++;
                            }
                        }
                    }

                    // create time
                    {
                        _lineIndex++;
                        List<CellData> cells = new List<CellData>();
                        cells.Add(new CellData(0, "创建时间"));
                        cells.Add(new CellData(1, strCreateTime));
                        doc.WriteExcelLine(_lineIndex, cells);

                        _lineIndex++;
                    }

                }
                finally
                {
                    doc.SaveWorksheet();
                    doc.Close();
                }
            }

            return 0;
        }


        static int ParseLines(string strInnerXml,
            out List<string> lines,
            out string strError)
        {
            lines = new List<string>();
            strError = "";

            XmlDocument dom = new XmlDocument();
            // dom.LoadXml("<root />");
            dom.AppendChild(dom.CreateElement("root"));

            try
            {
                dom.DocumentElement.InnerXml = strInnerXml;
            }
            catch (Exception ex)
            {
                strError = "InnerXml 装载时出错: " + ex.Message;
                return -1;
            }

            // TODO: 只有 <br /> 才分隔，其他的要联成一片
            foreach (XmlNode node in dom.DocumentElement.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Text)
                {
                    lines.Add(node.InnerText);
                }
            }

            return 0;
        }

        // 从 RML 文件中读入 <tr> 元素
        static int ReadLine(XmlTextReader reader,
            List<ColumnStyle> col_defs,
            out List<CellData> cells,
            out string strError)
        {
            strError = "";
            cells = new List<CellData>();
            int col_index = 0;

            int nColIndex = 0;
            while (true)
            {
                if (reader.Read() == false)
                    break;
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "th" || reader.Name == "td")
                    {
                        string strText = reader.ReadElementContentAsString();

                        CellData new_cell = null;

                        string strType = "";

                        // 2014/8/16
                        if (col_defs != null
                            && col_index < col_defs.Count)
                            strType = col_defs[col_index].Type;

                        if (strType == "String")
                            new_cell = new CellData(nColIndex++, strText, true, 0);
                        else if (strType == "Number")
                        {
                            new_cell = new CellData(nColIndex++, strText, false, 0);
                        }
                        else // "Auto")
                        {
                            bool isString = !IsExcelNumber(strText);

                            new_cell = new CellData(nColIndex++, strText, isString, 0);
                        }

                        cells.Add(new_cell);

                        col_index++;
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement
    && reader.Name == "tr")
                    break;
            }

            return 0;
        }

        // 检测字符串是否为纯数字(前面可以包含一个'-'号)
        public static bool IsExcelNumber(string s)
        {
            if (string.IsNullOrEmpty(s) == true)
                return false;

            bool bFoundNumber = false;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '-' && bFoundNumber == false)
                {
                    continue;
                }
                if (s[i] == '%' && i == s.Length - 1)
                {
                    // 最末一个字符为 %
                    continue;
                }
                if (s[i] > '9' || s[i] < '0')
                    return false;
                bFoundNumber = true;
            }
            return true;
        }


        #endregion

        #region RmlToHtml

        // RML 格式转换为 HTML 文件
        // parameters:
        //      strCssTemplate  CSS 模板。里面 %columns% 代表各列的样式
        public static int RmlToHtml(string strRmlFileName,
            string strHtmlFileName,
            string strCssTemplate,
            out string strError)
        {
            strError = "";
            int nRet = 0;
            try
            {
                using (Stream stream = File.OpenRead(strRmlFileName))
                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    while (true)
                    {
                        bool bRet = reader.Read();
                        if (bRet == false)
                        {
                            strError = "文件 " + strRmlFileName + " 没有根元素";
                            return -1;
                        }
                        if (reader.NodeType == XmlNodeType.Element)
                            break;
                    }

                    /*
                     * https://msdn.microsoft.com/en-us/library/system.xml.xmlwriter.writestring(v=vs.110).aspx
The default behavior of an XmlWriter created using Create is to throw an ArgumentException when attempting to write character values in the range 0x-0x1F (excluding white space characters 0x9, 0xA, and 0xD). These invalid XML characters can be written by creating the XmlWriter with the CheckCharacters property set to false. Doing so will result in the characters being replaced with numeric character entities (&#0; through &#0x1F). Additionally, an XmlTextWriter created with the new operator will replace the invalid characters with numeric character entities by default.
                     * */
                    using (XmlWriter writer = XmlWriter.Create(strHtmlFileName,
                        new XmlWriterSettings
                        {
                            Indent = true,
                            OmitXmlDeclaration = true,
                            CheckCharacters = false // 2016/6/3
                        }))
                    {
                        writer.WriteDocType("html", "-//W3C//DTD XHTML 1.0 Transitional//EN", "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd", null);
                        writer.WriteStartElement("html", "http://www.w3.org/1999/xhtml");
                        // writer.WriteAttributeString("xml", "lang", "", "en");

                        string strTitle = "";
                        string strComment = "";
                        string strCreateTime = "";
                        string strCss = "";
                        List<ColumnStyle> styles = null;

                        while (true)
                        {
                            bool bRet = reader.Read();
                            if (bRet == false)
                                break;
                            if (reader.NodeType == XmlNodeType.Element)
                            {
                                if (reader.Name == "title")
                                {
                                    strTitle = reader.ReadInnerXml();
                                }
                                else if (reader.Name == "comment")
                                {
                                    strComment = reader.ReadInnerXml();
                                }
                                else if (reader.Name == "createTime")
                                {
                                    strCreateTime = reader.ReadElementContentAsString();
                                }
                                else if (reader.Name == "style")
                                {
                                    strCss = reader.ReadElementContentAsString();
                                }
                                else if (reader.Name == "columns")
                                {
                                    // 从 RML 文件中读入 <columns> 元素
                                    nRet = ReadColumnStyle(reader,
                out styles,
                out strError);
                                    if (nRet == -1)
                                    {
                                        strError = "ReadColumnStyle() error : " + strError;
                                        return -1;
                                    }
                                }
                                else if (reader.Name == "table")
                                {
                                    writer.WriteStartElement("head");

                                    writer.WriteStartElement("meta");
                                    writer.WriteAttributeString("http-equiv", "Content-Type");
                                    writer.WriteAttributeString("content", "text/html; charset=utf-8");
                                    writer.WriteEndElement();

                                    // title
                                    {
                                        writer.WriteStartElement("title");
                                        // TODO 读入的时候直接形成 lines
                                        writer.WriteString(strTitle.Replace("<br />", " ").Replace("<br/>", " "));
                                        writer.WriteEndElement();
                                    }

                                    // css
                                    if (string.IsNullOrEmpty(strCss) == false)
                                    {
                                        writer.WriteStartElement("style");
                                        writer.WriteAttributeString("media", "screen");
                                        writer.WriteAttributeString("type", "text/css");
                                        writer.WriteString(strCss);
                                        writer.WriteEndElement();
                                    }

                                    // CSS 模板
                                    else if (string.IsNullOrEmpty(strCssTemplate) == false)
                                    {
                                        StringBuilder text = new StringBuilder();
                                        foreach (ColumnStyle style in styles)
                                        {
                                            string strAlign = style.Align;
                                            if (string.IsNullOrEmpty(strAlign) == true)
                                                strAlign = "left";
                                            text.Append("TABLE.table ." + style.Class + " {"
                                                + "text-align: " + strAlign + "; }\r\n");
                                        }

                                        writer.WriteStartElement("style");
                                        writer.WriteAttributeString("media", "screen");
                                        writer.WriteAttributeString("type", "text/css");
                                        writer.WriteString("\r\n" + strCssTemplate.Replace("%columns%", text.ToString()) + "\r\n");
                                        writer.WriteEndElement();
                                    }

                                    writer.WriteEndElement();   // </head>

                                    writer.WriteStartElement("body");

                                    if (string.IsNullOrEmpty(strTitle) == false)
                                    {
                                        writer.WriteStartElement("div");
                                        writer.WriteAttributeString("class", "tabletitle");
                                        writer.WriteRaw(strTitle);
                                        writer.WriteEndElement();
                                    }

                                    if (string.IsNullOrEmpty(strComment) == false)
                                    {
                                        writer.WriteStartElement("div");
                                        writer.WriteAttributeString("class", "titlecomment");
                                        writer.WriteRaw(strComment);
                                        writer.WriteEndElement();
                                    }

                                    // writer.WriteRaw(reader.ReadOuterXml());
                                    // DumpNode(reader, writer);
                                    writer.WriteNode(reader, true);

                                    {
                                        writer.WriteStartElement("div");
                                        writer.WriteAttributeString("class", "createtime");
                                        writer.WriteString("创建时间: " + strCreateTime);
                                        writer.WriteEndElement();
                                    }

                                    writer.WriteEndElement();   // </body>
                                }
                            }
                        }

                        writer.WriteEndElement();   // </html>
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                strError = "RmlToHtml() 出现异常: " + ExceptionUtil.GetDebugText(ex)
                    + "\r\nstrRmlFileName=" + strRmlFileName
                    + "\r\nstrHtmlFileName=" + strHtmlFileName
                    + "\r\nstrCssTemplate=" + strCssTemplate;
                throw new Exception(strError);
            }
        }


        class ColumnStyle
        {
            public string Class = "";
            public string Align = "";   // left/center/right
            public string Type = "";    // String/Currency/Auto/Number
        }

        // 从 RML 文件中读入 <columns> 元素
        static int ReadColumnStyle(XmlTextReader reader,
            out List<ColumnStyle> styles,
            out string strError)
        {
            strError = "";
            styles = new List<ColumnStyle>();

            while (true)
            {
                if (reader.Read() == false)
                    break;
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "column")
                    {
                        ColumnStyle style = new ColumnStyle();
                        style.Align = reader.GetAttribute("align");
                        style.Class = reader.GetAttribute("class");
                        style.Type = reader.GetAttribute("type");
                        styles.Add(style);
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement
    && reader.Name == "columns")
                    break;
            }

            return 0;
        }

        #endregion
    }

}
