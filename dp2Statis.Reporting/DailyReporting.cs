using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

using DigitalPlatform;
using DigitalPlatform.IO;
using DigitalPlatform.LibraryClientOpenApi;
using DigitalPlatform.LibraryServer.Reporting;
using DigitalPlatform.Text;
using DigitalPlatform.Xml;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.VariantTypes;
using Microsoft.EntityFrameworkCore;

namespace dp2Statis.Reporting
{
    // 构建每日报表
    public class DailyReporting
    {
        public DatabaseConfig? DatabaseConfig { get; set; }

        public ReportConfigBuilder? ReportConfigBuilder { get; set; }

        // // 这是一个 dp2Statid 实例的基础目录。在其下 reports 子目录中存放创建好的报表文件
        public string? BaseDirectory { get; set; }

        public string? ReportDefDirectory
        {
            get
            {
                return this.ReportConfigBuilder?.ReportDefDirectory;
            }
        }

        public void Initialize(DatabaseConfig databaseConfig,
            ReportConfigBuilder reportConfigBuilder,
            string baseDirectory)
        {
            this.DatabaseConfig = databaseConfig;
            this.ReportConfigBuilder = reportConfigBuilder;
            this.BaseDirectory = baseDirectory;
        }

        public class BuildPlanResult : NormalResult
        {
            public XmlDocument? Dom { get; set; }
        }


        // 创建每日报表执行计划
        // parameters:
        //      strRange    即将执行的每日报表的时间范围。
        //                  例如 20100101 或 20100101-20101231
        //                  注意起止日期均不能超过昨天(也就是说不能包含今天)。因为今天的操作日志可能还没有完全结束
        //                  可参考 dp2 源代码 GetDailyReportRangeString() 函数
        //      bFirst      是否为首次执行每日报表。
        //                  可根据上一次执行每日报表的日期，是否和 replication_task.xml 中的根元素 first_operlog_date 属性值一样，来进行判断。一样的就表示这是第一次
        public static BuildPlanResult BuildPlan(
            string strDailyEndDate, // 最近一次每日同步的最后日期
            string strRange,
            bool bFirst,
            List<string> librarycodes,
            CancellationToken token)
        {
            XmlDocument task_dom = new XmlDocument();
            task_dom.LoadXml("<root />");

            string strRealEndDate = "";

            var types = new List<string>
                {
                    "year",
                    "month",
                    "day"
                };

            foreach (string strLibraryCode in librarycodes)
            {
                token.ThrowIfCancellationRequested();

                XmlElement library_element = task_dom.CreateElement("library");
                task_dom.DocumentElement?.AppendChild(library_element);
                library_element.SetAttribute("code", strLibraryCode);

                foreach (string strTimeType in types)
                {
                    token.ThrowIfCancellationRequested();

                    // parameters:
                    //      strType 时间单位类型。 year month week day 之一
                    int nRet = GetTimePoints(
                        strDailyEndDate,
                        strTimeType,
                        strRange,   // strLastDay + "-" + strEndDay,
                        !bFirst,
                        out strRealEndDate,
                        out List<OneTime> times,
                        out string strError);
                    if (nRet == -1)
                        return new BuildPlanResult
                        {
                            Value = -1,
                            ErrorInfo = strError
                        };

                    foreach (OneTime time in times)
                    {
                        // stop.SetMessage("正在创建 " + GetDisplayLibraryCode(strLibraryCode) + " " + time.Time + " 的报表");
                        token.ThrowIfCancellationRequested();

                        bool bTailTime = false; // 是否为本轮最后一个(非探测)时间
                        if (times.IndexOf(time) == times.Count - 1)
                        {
                            if (time.Detect == false)
                                bTailTime = true;
                        }

                        XmlElement item_element = task_dom.CreateElement("item");
                        library_element.AppendChild(item_element);
                        item_element.SetAttribute("timeType", strTimeType);
                        item_element.SetAttribute("time", time.ToString());
                        // item_element.SetAttribute("times", OneTime.TimesToString(times));
                        item_element.SetAttribute("isTail", bTailTime ? "true" : "false");
                    }
                }
            }

            task_dom.DocumentElement?.SetAttribute("realEndDate", strRealEndDate);
            return new BuildPlanResult
            {
                Value = 0,
                Dom = task_dom
            };
        }

        // 要创建的文件类型
        [Flags]
        public enum FileType
        {
            RML = 0x01,
            HTML = 0x02,
            Excel = 0x04,
        }

        public delegate void Delegate_showText(string text);

        public class RunPlanResult : NormalResult
        {
            // 这个日期是上次处理完成的那一天的后一天，也就是说下次处理，从这天开始即可
            public string? EndDate { get; set; }
        }


        // 每日增量创建报表
        // return:
        //      -1  出错，或者中断
        //      0   没有任何配置的报表
        //      1   成功
        public RunPlanResult RunPlan(
            // DatabaseConfig databaseConfig,
            // ReportConfigBuilder _cfg,
            // string report_def_directory, _cfg.ReportDefDirectory
            ref XmlDocument task_dom,
            FileType _fileType,
            // string strBaseDirectory,    // 这是一个 dp2Statid 实例的基础目录。在其下 reports 子目录中存放创建好的报表文件
            Delegate_showText func_showText,
            CancellationToken token)
        {
            int nRet = 0;
            int nDoneCount = 0;
            string strError = "";

            ClearCache();

            // ClearCache();
            if (task_dom.DocumentElement?.GetIntegerParam(
                "doneCount",
                0,
                out nDoneCount,
                out strError) == -1)
                goto ERROR1;

            // var class_styles = GetClassFromStyles(task_dom.DocumentElement);
            /*

            // 从计划文件获得所有分类号检索途径 style
            nRet = GetClassFromStylesFromFile(
                out class_styles,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            */

            // string strRealEndDate = "";
            bool bFoundReports = false;

            /*
            this.EnableControls(false);

            _stop.OnStop += new StopEventHandler(this.DoStop);
            _stop.Initial("正在创建报表 ...");
            _stop.BeginLoop();
            */
            func_showText?.Invoke("正在创建报表 ...");
            try
            {
                /*
                // 创建必要的索引
                this._connectionString = GetOperlogConnectionString();
                func_showText?.Invoke("正在检查和创建 SQL 索引 ...");
                foreach (string type in OperLogTable.DbTypes)
                {
                    // Application.DoEvents();

                    nRet = OperLogTable.CreateAdditionalIndex(
                        type,
                        this._connectionString,
                        out strError);
                    if (nRet == -1)
                        goto ERROR1;
                }

                // 删除所有先前复制出来的 class 表
                nRet = DeleteAllDistinctClassTable(out strError);
                if (nRet == -1)
                    goto ERROR1;
                */

                var all_item_nodes = task_dom.DocumentElement?
                    .SelectNodes("library/item");
                // looping.Progress.SetProgressRange(0, all_item_nodes.Count + nDoneCount);

                //_estimate.SetRange(0, all_item_nodes.Count + nDoneCount);
                //_estimate.StartEstimate();

                var library_nodes = task_dom.DocumentElement?
                    .SelectNodes("library");
                int i = nDoneCount;
                // looping.Progress.SetProgressValue(i);
                foreach (XmlElement library_element in library_nodes)
                {
                    token.ThrowIfCancellationRequested();

                    string strLibraryCode = library_element.GetAttribute("code");

                    var item_nodes = library_element.SelectNodes("item");

                    foreach (XmlElement item_element in item_nodes)
                    {
                        string strTimeType = item_element.GetAttribute("timeType");
                        OneTime time = OneTime.FromString(item_element.GetAttribute("time"));
                        // List<OneTime> times = OneTime.TimesFromString(item_element.GetAttribute("times"));
                        bool bTailTime = ElementExtension.IsBooleanTrue(item_element.GetAttribute("isTail"));
                        List<string>? report_names = StringUtil.SplitList(item_element.GetAttribute("reportNames"), "|||");
                        if (report_names.Count == 0)
                            report_names = null;

                        func_showText?.Invoke("正在创建 " + GetDisplayLibraryCode(strLibraryCode) + " " + time.Time + " 的报表。"/* + GetProgressTimeString(i)*/);

                        token.ThrowIfCancellationRequested();

                        // return:
                        //      -1  出错
                        //      0   没有任何匹配的报表
                        //      1   成功处理
                        var ret = CreateOneTimeReports(
                            //databaseConfig,
                            //report_def_directory,
                            //(ReportConfigBuilder)_cfg,
                            token,
                            //strBaseDirectory,
                            strTimeType,
                            time,
                            bTailTime,
                            // times,
                            strLibraryCode,
                            report_names,
                            // class_styles,
                            _fileType,
                            func_showText);
                        if (ret.Value == -1)
                        {
                            strError = ret.ErrorInfo;
                            goto ERROR1;
                        }
                        if (ret.Value == 1)
                            bFoundReports = true;

                        item_element.ParentNode?.RemoveChild(item_element);  // 做过的报表事项, 从 task_dom 中删除
                        nDoneCount++;

                        i++;
                        // looping.Progress.SetProgressValue(i);
                    }

                    // fileType 没有 html 的时候，不要创建 index.html 文件
                    if ((_fileType & FileType.HTML) != 0)
                    {
                        if (this.BaseDirectory == null)
                            throw new ArgumentException("this.BaseDirectory == null");

                        string strOutputDir = GetReportOutputDir(this.BaseDirectory, strLibraryCode);
                        string strIndexXmlFileName = Path.Combine(strOutputDir, "index.xml");
                        string strIndexHtmlFileName = Path.Combine(strOutputDir, "index.html");

                        func_showText?.Invoke("正在创建 " + strIndexHtmlFileName);

                        // 根据 index.xml 文件创建 index.html 文件
                        nRet = CreateIndexHtmlFile(
                            _fileType,
                            strIndexXmlFileName,
                            strIndexHtmlFileName,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                }

                /*
                // 删除所有先前复制出来的 class 表
                nRet = DeleteAllDistinctClassTable(out strError);
                if (nRet == -1)
                    goto ERROR1;
                */
            }
            finally
            {
                task_dom.DocumentElement?.SetAttribute("doneCount", nDoneCount.ToString());
            }

            // ShrinkIndexCache(true);

            var strRealEndDate = task_dom.DocumentElement?
                .GetAttribute("realEndDate");

            if (string.IsNullOrEmpty(strRealEndDate) == false)
            {
                /*
                // 这个日期是上次处理完成的那一天的后一天，也就是说下次处理，从这天开始即可
                Program.MainForm.AppInfo.SetString(GetReportSection(),
                    "daily_report_end_date",
                    GetNextDate(strRealEndDate));
                */
            }

            if (bFoundReports == false)
            {
                strError = "当前没有任何报表配置可供创建报表。请先去“报表配置”属性页配置好各个分馆的报表";
                return new RunPlanResult
                {
                    Value = 0,
                    EndDate = GetNextDate(strRealEndDate)
                };
            }

            // 把若干 index.xml 文件兑现保存到磁盘
            ShrinkIndexCache(true);

            /*
            this.Invoke((Action)(() =>
            Program.MainForm.StatusBarMessage = "耗费时间 " + ProgressEstimate.Format(_estimate.delta_passed)
));
            */
            return new RunPlanResult
            {
                Value = 1,
                EndDate = GetNextDate(strRealEndDate)
            };
        ERROR1:
            ShrinkIndexCache(true);
            return new RunPlanResult
            {
                Value = -1,
                ErrorInfo = strError
            };
        }

        // 从计划文件中获得所有分类号检索途径 style
        List<BiblioDbFromInfo>? GetClassFromStyles(
            XmlElement? root)
        {
            if (root == null)
                return null;
            var nodes = root.SelectNodes("classStyles/style");
            return nodes?.Cast<XmlElement>()
                // .AsQueryable<XmlElement>()
                .Select(o => new BiblioDbFromInfo
                {
                    Caption = o.GetAttribute("caption"),
                    Style = o.GetAttribute("style")
                })
                .ToList();

            /*
            var styles = new List<BiblioDbFromInfo>();
            foreach (XmlElement element in nodes)
            {
                BiblioDbFromInfo info = new BiblioDbFromInfo();
                info.Caption = element.GetAttribute("caption");
                info.Style = element.GetAttribute("style");
                styles.Add(info);
            }
            return 0;
            */
        }

        // 创建一个特定时间段(一个分馆)的若干报表
        // 要将创建好的报表写入相应目录的 index.xml 中
        // parameters:
        //      strTimeType 时间单位类型。 year month week day 之一
        //      times   本轮的全部时间字符串。strTime 一定在其中。通过 times 和 strTime，能看出 strTime 时间是不是数组最后一个元素
        // return:
        //      -1  出错
        //      0   没有任何匹配的报表
        //      1   成功处理
        NormalResult CreateOneTimeReports(
            // DatabaseConfig databaseConfig,
            // string report_def_directory,
            // ReportConfigBuilder _cfg,
            CancellationToken token,
            // string strBaseDirectory,
            string strTimeType,
            OneTime time,
            bool bTailTime,
            // List<OneTime> times,
            string strLibraryCode,
            List<string> report_names,
            // List<BiblioDbFromInfo> class_styles,
            FileType fileType,
            Delegate_showText? func_showText = null)
        {
            string strError = "";
            int nRet = 0;

            if (this.BaseDirectory == null)
                throw new ArgumentException("this.BaseDirectory == null");

            // 特定分馆的报表输出目录
            // string strReportsDir = Path.Combine(Program.MainForm.UserDir, "reports/" + (string.IsNullOrEmpty(strLibraryCode) == true ? "global" : strLibraryCode));
            string strReportsDir = GetReportOutputDir(this.BaseDirectory, strLibraryCode);
            OperLogLoader.TryCreateDir(strReportsDir);

            // 输出文件目录
            // string strOutputDir = Path.Combine(strReportsDir, time.Time);
            // 延迟到创建表格的时候创建子目录

            string strOutputDir = Path.Combine(strReportsDir, GetValidPathString(GetSubDirName(time.Time)));

            // 看看目录是否已经存在
            if (time.Detect)
            {
                DirectoryInfo di = new DirectoryInfo(strOutputDir);
                if (di.Exists == true)
                {
                    func_showText?.Invoke($"{time.Time} (馆代码: '{strLibraryCode}')的输出目录已经存在，跳过处理");
                    return new NormalResult();
                }
            }


            //foreach (string strLibraryCode in librarycodes)
            //{

            if (this.ReportConfigBuilder == null)
                throw new ArgumentException("this.ReportConfigBuilder == null");

            var nodeLibrary = this.ReportConfigBuilder?.GetLibraryNode(strLibraryCode);
            if (nodeLibrary == null)
            {
                strError = "在配置文件中没有找到馆代码为 '" + strLibraryCode + "' 的 <library> 元素";
                goto ERROR1;
            }

            string strTemplate = "";

            if (this.ReportDefDirectory == null)
                throw new ArgumentException("this.ReportDefDirectory == null");

            nRet = ReportConfigBuilder.LoadHtmlTemplate(
                nodeLibrary,
                this.ReportDefDirectory,
                out strTemplate,
                out strError);
            if (nRet == -1)
                goto ERROR1;
            var _cssTemplate = strTemplate;

            var report_nodes = new List<XmlElement>();
            if (report_names != null)
            {
                foreach (string strName in report_names)
                {
                    var node = nodeLibrary.SelectSingleNode("reports/report[@name='" + strName + "']") as XmlElement;
                    if (node == null)
                    {
                        continue;
#if NO
                        strError = "在配置文件中没有找到馆代码为 '" + strLibraryCode + "' 的 <library> 元素下的 name 属性值为 '"+strName+"' 的 report 元素";
                        return -1;
                        // TODO: 出现 MessageBox 警告，但可以选择继续
#endif
                    }
                    report_nodes.Add(node);
                }

                if (report_nodes.Count == 0)
                    return new NormalResult();   // 没有任何匹配的报表
            }
            else
            {
                var nodes = nodeLibrary.SelectNodes("reports/report");
                if (nodes == null || nodes?.Count == 0)
                    return new NormalResult();   // 这个分馆 当前没有配置任何报表
                if (nodes != null)
                {
                    foreach (XmlElement node in nodes)
                    {
                        report_nodes.Add(node);
                    }
                }
            }

            foreach (var node in report_nodes)
            {
                token.ThrowIfCancellationRequested();

                string strName = node.GetAttribute("name");
                string strReportType = node.GetAttribute("type");
                string strCfgFile = node.GetAttribute("cfgFile");
                string strNameTable = node.GetAttribute("nameTable");
                string strFreq = node.GetAttribute("frequency");


                // *** 判断频率
                if (report_names == null)
                {
                    if (StringUtil.IsInList(strTimeType, strFreq) == false)
                    {
                        func_showText?.Invoke($"频率 {strTimeType} 未被 {strReportType} 选择，跳过处理");
                        continue;
                    }
                }

                nRet = GetReportWriter(strCfgFile,
    out ReportWriter? writer,
    out strError);
                if (nRet == -1)
                    goto ERROR1;

                if (writer == null)
                {
                    strError = "writer == null";
                    goto ERROR1;
                }

                // 在指定了报表名称列表的情况下，频率不再筛选

                // 判断新鲜程度。只有本轮最后一次才创建报表
#if NO
                if (config.Fresh == true && bTailTime == false)
                    continue;
#endif
                if (writer.GetFresh() == true && bTailTime == false)
                {
                    func_showText?.Invoke($"报表 {strReportType} 的时段 {time.Time} 不是新鲜时段，跳过处理");
                    continue;
                }

                Hashtable macro_table = new Hashtable();
                macro_table["%library%"] = strLibraryCode;
                string strOutputFileName = Path.Combine(strOutputDir, Guid.NewGuid().ToString() + ".rml");

                string strDoneName = strLibraryCode + "|"
                    + time.Time + "|"
                    + strName + "|"
                    + strReportType + "|";
#if NO
                if (this._doneTable.ContainsKey(strDoneName) == true)
                    continue;   // 前次已经做过了
#endif
                int nAdd = 0;   // 0 表示什么也不做。 1表示要加入 -1 表示要删除

                if (false
                    //strReportType == "102" || strReportType == "9102"
                    )
                {
#if TEMP
                    // *** 102
                    // 按照指定的单位名称列表，列出借书册数
                    nRet = Create_102_report(
                        stop,
                        strLibraryCode,
                        time.Time,
                        strCfgFile,
                        // "选定的部门",    // 例如： 各年级
                        macro_table,
                        strNameTable,
                        strOutputFileName,
                        strReportType,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                        nAdd = -1;
                    else if (nRet == 1)
                        nAdd = 1;
#endif
                }
                else if (strReportType == "101" || strReportType == "9101"
                    || strReportType == "111" || strReportType == "9111"
                    || strReportType == "121" || strReportType == "9121"
                    || strReportType == "122" || strReportType == "9122"
                    || strReportType == "141"
                    || strReportType == "102" || strReportType == "9102")
                {
                    Hashtable param_table = new Hashtable();
                    // string strDateRange = param_table["dateRange"] as string;
                    param_table["dateRange"] = time.Time;
                    param_table["libraryCode"] = strLibraryCode;

                    if (strReportType == "102" || strReportType == "9102")
                        param_table["departments"] = strNameTable;

                    writer.Algorithm = strReportType;

                    if (this.DatabaseConfig == null)
                        throw new ArgumentException("this.DatabaseConfig == null");

                    using (var context = new LibraryContext(this.DatabaseConfig))
                    {
                        // return:
                        //      -1  出错
                        //      0   没有创建文件(因为输出的表格为空)
                        //      >=1   成功创建文件
                        nRet = Report.BuildReport(context,
                            param_table,
                            // dlg.Parameters,
                            writer,
                            strOutputFileName);
                        if (nRet == -1)
                        {
                            strError = "Report.BuildReport() error";
                            goto ERROR1;
                        }
                        if (nRet == 0)
                            nAdd = -1;
                        else if (nRet >= 1)
                            nAdd = 1;
                    }

                    /*
                    nRet = Create_1XX_report(
                        stop,
                        strLibraryCode,
                        time.Time,
                        strCfgFile,
                        macro_table,
                        strOutputFileName,
                        strReportType,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                        nAdd = -1;
                    else if (nRet == 1)
                        nAdd = 1;
                    */
                }
                else if (strReportType == "131" || strReportType == "9131")
                {
                    Hashtable param_table = new Hashtable();
                    // string strDateRange = param_table["dateRange"] as string;
                    param_table["dateRange"] = time.Time;
                    param_table["libraryCode"] = strLibraryCode;

                    writer.Algorithm = strReportType;

                    if (this.DatabaseConfig == null)
                        throw new ArgumentException("this.DatabaseConfig == null");

                    string str131Dir = Path.Combine(strOutputDir, "table_" + strReportType);
                    using (var context = new LibraryContext(this.DatabaseConfig))
                    {

                        // return:
                        //      -1  出错
                        //      0   没有创建目录
                        //      >=1   创建了目录
                        nRet = BuildAllReport_131(context,
                            param_table,
                            strCfgFile,
                            str131Dir,
                            strReportType,
                            fileType,
                            _cssTemplate,
                            token,
                            out strError);
                    }

                    {
                        // 将 131 目录事项写入 index.xml
                        nRet = WriteIndexXml(
                            fileType,
                            strTimeType,
                            time.Time,
                            strName,
                            "", // strReportsDir,
                            str131Dir,
                            strReportType,
                            nRet >= 1,
                            _cssTemplate,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }

                }
                else if (strReportType == "201" || strReportType == "9201"
    || strReportType == "202" || strReportType == "9202"
    || strReportType == "212" || strReportType == "9212"
    || strReportType == "213") // begin of 2xx
                {
                    /*
                    if ((strReportType == "212" || strReportType == "9212")
                        && class_styles.Count == 0)
                        continue;
                    */
                    if (strReportType == "213")
                    {
                        func_showText?.Invoke("213 表已经被废止。跳过处理");
                        continue;   // 213 表已经被废止，其原有功能被合并到 212 表
                    }

                    // 获得分馆的所有馆藏地点
                    var locations = GetLibraryLocation(strLibraryCode);
                    /*
                    List<string> locations = null;
                    locations = this._libraryLocationCache.FindObject(strLibraryCode);
                    if (locations == null)
                    {
                        nRet = GetAllItemLocations(
                            strLibraryCode,
                            true,
                            out locations,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        this._libraryLocationCache.SetObject(strLibraryCode, locations);
                    }
                    */

                    int iLocation = 0;
                    foreach (string strLocation in locations)
                    {
                        token.ThrowIfCancellationRequested();

                        func_showText?.Invoke("正在为馆藏地 '" + strLocation + "' 创建 " + strReportType + " 报表 (" + (iLocation + 1) + "/" + locations.Count + ")");

                        // macro_table["%location%"] = GetLocationCaption(strLocation);

                        // 这里稍微特殊一点，循环要写入多个输出文件
                        if (string.IsNullOrEmpty(strOutputFileName) == true)
                            strOutputFileName = Path.Combine(strOutputDir, Guid.NewGuid().ToString() + ".rml");

                        if (strReportType == "201" || strReportType == "9201"
                            || strReportType == "202" || strReportType == "9202")
                        {
                            Hashtable param_table = new Hashtable();
                            // string strDateRange = param_table["dateRange"] as string;
                            param_table["dateRange"] = time.Time;
                            param_table["libraryCode"] = strLibraryCode;

                            param_table["location"] = strLocation;
                            writer.Algorithm = strReportType;

                            using (var context = new LibraryContext(this.DatabaseConfig))
                            {
                                // return:
                                //      -1  出错
                                //      0   没有创建文件(因为输出的表格为空)
                                //      >=1   成功创建文件
                                nRet = Report.BuildReport(
                                context,
                                param_table,
                                // strLocation,
                                // time.Time,
                                // strCfgFile,
                                // macro_table,
                                writer,
                                strOutputFileName);
                                if (nRet == -1)
                                {
                                    strError = "Report.BuildReport() error";
                                    goto ERROR1;
                                }
                            }
                        }
                        else if (strReportType == "212" || strReportType == "9212"
                            || strReportType == "213" || strReportType == "9213")
                        {
                            // List<string> names = StringUtil.SplitList(strNameTable);
                            nRet = OneClassType.BuildClassTypes(strNameTable,
            out List<OneClassType> class_table,
            out strError);
                            if (nRet == -1)
                            {
                                strError = "报表类型 '" + strReportType + "' 的名字表定义不合法： " + strError;
                                goto ERROR1;
                            }

                            if (class_table.Count == 0)
                            {
                                strError = "报表类型 '" + strReportType + "' 没有定义名字表，无法进行统计";
                                goto ERROR1;
                            }

                            // foreach (BiblioDbFromInfo style in class_styles)
                            foreach (var current_type in class_table)
                            {
                                token.ThrowIfCancellationRequested();

#if NO
                                if (names.Count > 0)
                                {
                                    // 只处理设定的那些 class styles
                                    if (names.IndexOf(style.Style) == -1)
                                        continue;
                                }
#endif
                                /*
                                OneClassType? current_type = null;
                                if (class_table.Count > 0)
                                {
                                    // 只处理设定的那些 class styles
                                    int index = OneClassType.IndexOf(class_table, style.Style);
                                    if (index == -1)
                                        continue;
                                    current_type = class_table[index];
                                }
                                */

                                // 这里稍微特殊一点，循环要写入多个输出文件
                                if (string.IsNullOrEmpty(strOutputFileName) == true)
                                    strOutputFileName = Path.Combine(strOutputDir, Guid.NewGuid().ToString() + ".rml");

                                /*
                                nRet = Create_212_report(
                                    stop,
                                    strLocation,
                                    style.Style,
                                    style.Caption,
                                    time.Time,
                                    strCfgFile,
                                    macro_table,
                                    current_type == null ? null : current_type.Filters,
                                    strOutputFileName,
                                    strReportType,
                                    out strError);
                                if (nRet == -1)
                                    return -1;
                                */
                                Hashtable param_table = new Hashtable();
                                // string strDateRange = param_table["dateRange"] as string;
                                param_table["dateRange"] = time.Time;
                                param_table["libraryCode"] = strLibraryCode;

                                param_table["location"] = strLocation;
                                // classType ?
                                param_table["classType"] = current_type == null ? null : current_type.ClassType;
                                writer.Algorithm = strReportType;

                                using (var context = new LibraryContext(this.DatabaseConfig))
                                {
                                    // return:
                                    //      -1  出错
                                    //      0   没有创建文件(因为输出的表格为空)
                                    //      >=1   成功创建文件
                                    nRet = Report.BuildReport(context,
                                    param_table,
                                    // dlg.Parameters,
                                    writer,
                                    strOutputFileName);
                                    if (nRet == -1)
                                    {
                                        strError = "Report.BuildReport() error";
                                        goto ERROR1;
                                    }

                                    // if (nRet == 1)
                                    {
                                        // 写入 index.xml
                                        nRet = WriteIndexXml(
                                            fileType,
                                            strTimeType,
                                            time.Time,
                                            GetLocationCaption(strLocation) + "-" + current_type.ClassType, // style.Caption,  // 把名字区别开。否则写入 <report> 会重叠覆盖
                                            strName,    // strReportsDir,
                                            strOutputFileName,
                                            strReportType,
                                            nRet >= 1,
                                            _cssTemplate,
                                            out strError);
                                        if (nRet == -1)
                                            goto ERROR1;

                                        strOutputFileName = "";
                                    }
                                }
                            }
                        }

                        if (File.Exists(strOutputFileName) == true)
                        {
                            // 写入 index.xml
                            nRet = WriteIndexXml(
                                fileType,
                                strTimeType,
                                time.Time,
                                GetLocationCaption(strLocation),  // 把名字区别开。否则写入 <report> 会重叠覆盖
                                strName,    // strReportsDir,
                                strOutputFileName,
                                strReportType,
                                true,
                                _cssTemplate,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            strOutputFileName = "";
                        }

                        iLocation++;
                    }

                    // TODO: 总的馆藏地点还要来一次

                } // end 2xx
                else if (strReportType == "301"
    || strReportType == "302") // begin of 3xx
                {
                    // 获得分馆的所有馆藏地点
                    // List<string> locations = null;
                    var locations = GetLibraryLocation(strLibraryCode);
                    /*
                    var locations = this._libraryLocationCache.FindObject(strLibraryCode);
                    if (locations == null)
                    {
                        locations = GetAllItemLocations(
                            strLibraryCode,
                            true);
                        this._libraryLocationCache.SetObject(strLibraryCode, locations);
                    }
                    */

                    int iLocation = 0;
                    foreach (string strLocation in locations)
                    {
                        token.ThrowIfCancellationRequested();

                        func_showText?.Invoke("正在为馆藏地 '" + strLocation + "' 创建 " + strReportType + " 报表 (" + (iLocation + 1) + "/" + locations.Count + ")");

                        macro_table["%location%"] = GetLocationCaption(strLocation);

                        // 这里稍微特殊一点，循环要写入多个输出文件
                        if (string.IsNullOrEmpty(strOutputFileName) == true)
                            strOutputFileName = Path.Combine(strOutputDir, Guid.NewGuid().ToString() + ".rml");

                        if (strReportType == "301"
                            || strReportType == "302")
                        {
                            // List<string> names = StringUtil.SplitList(strNameTable);
                            nRet = OneClassType.BuildClassTypes(strNameTable,
            out List<OneClassType> class_table,
            out strError);
                            if (nRet == -1)
                            {
                                strError = "报表类型 '" + strReportType + "' 的名字表定义不合法： " + strError;
                                goto ERROR1;
                            }

                            if (class_table.Count == 0)
                            {
                                strError = "报表类型 '" + strReportType + "' 没有定义名字表，无法进行统计";
                                goto ERROR1;
                            }

                            // foreach (BiblioDbFromInfo style in class_styles)
                            foreach (var current_type in class_table)
                            {
                                token.ThrowIfCancellationRequested();

                                /*
                                OneClassType current_type = null;
                                if (class_table.Count > 0)
                                {
                                    // 只处理设定的那些 class styles
                                    int index = OneClassType.IndexOf(class_table, style.Style);
                                    if (index == -1)
                                        continue;
                                    current_type = class_table[index];
                                }
                                */

                                // 这里稍微特殊一点，循环要写入多个输出文件
                                if (string.IsNullOrEmpty(strOutputFileName) == true)
                                    strOutputFileName = Path.Combine(strOutputDir, Guid.NewGuid().ToString() + ".rml");

#if REMOVED
                                if (strReportType == "301")
                                    nRet = Create_301_report(
                                        stop,
                                        strLocation,
                                        style.Style,
                                        style.Caption,
                                        time.Time,
                                        strCfgFile,
                                        macro_table,
                                        current_type == null ? null : current_type.Filters,
                                        strOutputFileName,
                                        out strError);
                                else if (strReportType == "302")
                                    nRet = Create_302_report(
                                        stop,
                                        strLocation,
        style.Style,
        style.Caption,
        time.Time,
        strCfgFile,
        macro_table,
                                        current_type == null ? null : current_type.Filters,
        strOutputFileName,
        out strError);
                                if (nRet == -1)
                                    return -1;
#endif
                                Hashtable param_table = new Hashtable();
                                param_table["dateRange"] = time.Time;
                                // param_table["libraryCode"] = strLibraryCode;

                                param_table["location"] = strLocation;
                                param_table["classType"] = current_type == null ? null : current_type.ClassType;
                                writer.Algorithm = strReportType;

                                using (var context = new LibraryContext(this.DatabaseConfig))
                                {
                                    // return:
                                    //      -1  出错
                                    //      0   没有创建文件(因为输出的表格为空)
                                    //      >=1   成功创建文件
                                    nRet = Report.BuildReport(context,
                                    param_table,
                                    // dlg.Parameters,
                                    writer,
                                    strOutputFileName);
                                    if (nRet == -1)
                                    {
                                        strError = "Report.BuildReport() error";
                                        goto ERROR1;
                                    }

                                    // if (nRet == 1)
                                    {
                                        // 写入 index.xml
                                        nRet = WriteIndexXml(
                                            fileType,
                                            strTimeType,
                                            time.Time,
                                            GetLocationCaption(strLocation) + "-" + current_type.ClassType, // style.Caption,  // 把名字区别开。否则写入 <report> 会重叠覆盖
                                            strName,    // strReportsDir,
                                            strOutputFileName,
                                            strReportType,
                                            nRet >= 1,
                                            _cssTemplate,
                                            out strError);
                                        if (nRet == -1)
                                            goto ERROR1;

                                        strOutputFileName = "";
                                    }
                                }


                                /*
                                // if (nRet == 1)
                                {
                                    // 写入 index.xml
                                    nRet = WriteIndexXml(
                                        strTimeType,
                                        time.Time,
                                        GetLocationCaption(strLocation) + "-" + style.Caption,  // 把名字区别开。否则写入 <report> 会重叠覆盖
                                        strName,    // strReportsDir,
                                        strOutputFileName,
                                        strReportType,
                             nRet == 1,
                                       out strError);
                                    if (nRet == -1)
                                        return -1;

                                    strOutputFileName = "";
                                }
                                */
                            }
                        }

                        if (File.Exists(strOutputFileName) == true)
                        {
                            // 写入 index.xml
                            nRet = WriteIndexXml(
                                fileType,
                                strTimeType,
                                time.Time,
                                GetLocationCaption(strLocation),  // 把名字区别开。否则写入 <report> 会重叠覆盖
                                strName,    // strReportsDir,
                                strOutputFileName,
                                strReportType,
                                true,
                                _cssTemplate,
                                out strError);
                            if (nRet == -1)
                                goto ERROR1;

                            strOutputFileName = "";
                        }

                        iLocation++;
                    }

                    // TODO: 总的馆藏地点还要来一次
                } // end 3xx
                else if (strReportType == "411"
    || strReportType == "412"
    || strReportType == "421"
    || strReportType == "422"
    || strReportType == "431"
    || strReportType == "432"
    || strReportType == "441"
    || strReportType == "442"
    || strReportType == "443"
    || strReportType == "451"
    || strReportType == "452"
    || strReportType == "471"
    || strReportType == "472"
    || strReportType == "481"
    || strReportType == "482"
    || strReportType == "491"
    || strReportType == "492"
    )
                {
                    Hashtable param_table = new Hashtable();
                    param_table["dateRange"] = time.Time;
                    param_table["libraryCode"] = strLibraryCode;

                    writer.Algorithm = strReportType;

                    if (this.DatabaseConfig == null)
                        throw new ArgumentException("this.DatabaseConfig == null");

                    using (var context = new LibraryContext(this.DatabaseConfig))
                    {
                        // return:
                        //      -1  出错
                        //      0   没有创建文件(因为输出的表格为空)
                        //      >=1   成功创建文件
                        nRet = Report.BuildReport(context,
                            param_table,
                            // dlg.Parameters,
                            writer,
                            strOutputFileName);
                        if (nRet == -1)
                        {
                            strError = "Report.BuildReport() error";
                            goto ERROR1;
                        }
                        if (nRet == 0)
                            nAdd = -1;
                        else if (nRet >= 1)
                            nAdd = 1;
                    }

                    /*
                    nRet = Create_4XX_report(
                        stop,
                        strLibraryCode,
                        time.Time,
                        strCfgFile,
                        macro_table,
                        strOutputFileName,
                        strReportType,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    if (nRet == 0)
                        nAdd = -1;
                    else if (nRet == 1)
                        nAdd = 1;
                    */


                }
                else if (strReportType == "493")
                {
#if NO
                        // 这里稍微特殊一点，循环要写入多个输出文件
                        if (string.IsNullOrEmpty(strOutputFileName) == true)
                            strOutputFileName = Path.Combine(strOutputDir, Guid.NewGuid().ToString() + ".rml");
#endif
                    nRet = OneClassType.BuildClassTypes(strNameTable,
    out List<OneClassType> class_table,
    out strError);
                    if (nRet == -1)
                    {
                        strError = "报表类型 '" + strReportType + "' 的名字表定义不合法： " + strError;
                        goto ERROR1;
                    }

                    if (class_table.Count == 0)
                    {
                        strError = "报表类型 '" + strReportType + "' 没有定义名字表，无法进行统计";
                        goto ERROR1;
                    }

                    // foreach (BiblioDbFromInfo style in class_styles)
                    foreach (var current_type in class_table)
                    {
                        token.ThrowIfCancellationRequested();

                        /*
                        OneClassType current_type = null;
                        if (class_table.Count > 0)
                        {
                            // 只处理设定的那些 class styles
                            int index = OneClassType.IndexOf(class_table, style.Style);
                            if (index == -1)
                                continue;
                            current_type = class_table[index];
                        }
                        */

                        // 这里稍微特殊一点，循环要写入多个输出文件
                        if (string.IsNullOrEmpty(strOutputFileName) == true)
                            strOutputFileName = Path.Combine(strOutputDir, Guid.NewGuid().ToString() + ".rml");

                        /*
                        nRet = Create_493_report(
                            stop,
                            strLibraryCode,
                            style.Style,
                            style.Caption,
                            time.Time,
                            strCfgFile,
                            macro_table,
                            current_type == null ? null : current_type.Filters,
                            strOutputFileName,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        */

                        Hashtable param_table = new Hashtable();
                        param_table["dateRange"] = time.Time;
                        param_table["libraryCode"] = strLibraryCode;

                        param_table["classType"] = current_type == null ? null : current_type.ClassType;
                        writer.Algorithm = strReportType;

                        using (var context = new LibraryContext(this.DatabaseConfig))
                        {
                            // return:
                            //      -1  出错
                            //      0   没有创建文件(因为输出的表格为空)
                            //      >=1   成功创建文件
                            nRet = Report.BuildReport(context,
                            param_table,
                            // dlg.Parameters,
                            writer,
                            strOutputFileName);
                            if (nRet == -1)
                            {
                                strError = "Report.BuildReport() error";
                                goto ERROR1;
                            }

                            // if (nRet == 1)
                            {
                                // 写入 index.xml
                                nRet = WriteIndexXml(
                                    fileType,
                                    strTimeType,
                                    time.Time,
                                    current_type.ClassType,
                                    strName,    // strReportsDir,
                                    strOutputFileName,
                                    strReportType,
                                    nRet >= 1,
                                    _cssTemplate,
                                    out strError);
                                if (nRet == -1)
                                    goto ERROR1;

                                strOutputFileName = "";
                            }
                        }

                        /*
                        // if (nRet == 1)
                        {
                            // 写入 index.xml
                            nRet = WriteIndexXml(
                                strTimeType,
                                time.Time,
                                style.Caption,  // 把名字区别开。否则写入 <report> 会重叠覆盖
                                strName,    // strReportsDir,
                                strOutputFileName,
                                strReportType,
                     nRet == 1,
                               out strError);
                            if (nRet == -1)
                                return -1;

                            strOutputFileName = "";
                        }
                        */
                    }

                }

#if TEMP

                else if (strReportType == "131" || strReportType == "9131")
                {
                    string str131Dir = Path.Combine(strOutputDir, "table_" + strReportType);
                    // 这是创建到一个子目录(会在子目录中创建很多文件和下级目录)，而不是输出到一个文件
                    nRet = Create_131_report(
                        stop,
                        strLibraryCode,
                        time.Time,
                        strCfgFile,
                        macro_table,
                        str131Dir,
                        strReportType,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    // if (nRet == 1)
                    {
                        // 将 131 目录事项写入 index.xml
                        nRet = WriteIndexXml(
                            strTimeType,
                            time.Time,
                            strName,
                            "", // strReportsDir,
                            str131Dir,
                            strReportType,
                            nRet == 1,
                            out strError);
                        if (nRet == -1)
                            return -1;
                    }
                }
                else if (strReportType == "201" || strReportType == "9201"
                    || strReportType == "202" || strReportType == "9202"
                    || strReportType == "212" || strReportType == "9212"
                    || strReportType == "213") // begin of 2xx
                {
                    if ((strReportType == "212" || strReportType == "9212")
                        && class_styles.Count == 0)
                        continue;
                    if (strReportType == "213")
                        continue;   // 213 表已经被废止，其原有功能被合并到 212 表

                    // 获得分馆的所有馆藏地点

                    List<string> locations = null;
                    locations = this._libraryLocationCache.FindObject(strLibraryCode);
                    if (locations == null)
                    {
                        nRet = GetAllItemLocations(
                            strLibraryCode,
                            true,
                            out locations,
                            out strError);
                        if (nRet == -1)
                            return -1;
                        this._libraryLocationCache.SetObject(strLibraryCode, locations);
                    }

                    int iLocation = 0;
                    foreach (string strLocation in locations)
                    {
                        if (this.InvokeRequired == false)
                            Application.DoEvents();
                        if (stop != null && stop.State != 0)
                        {
                            strError = "中断";
                            return -1;
                        }

                        this.ShowMessage("正在为馆藏地 '" + strLocation + "' 创建 " + strReportType + " 报表 (" + (iLocation + 1) + "/" + locations.Count + ")");

                        macro_table["%location%"] = GetLocationCaption(strLocation);

                        // 这里稍微特殊一点，循环要写入多个输出文件
                        if (string.IsNullOrEmpty(strOutputFileName) == true)
                            strOutputFileName = Path.Combine(strOutputDir, Guid.NewGuid().ToString() + ".rml");

                        if (strReportType == "201" || strReportType == "9201")
                        {
                            nRet = Create_201_report(
                                stop,
                                strLocation,
                                time.Time,
                                strCfgFile,
                                macro_table,
                                strOutputFileName,
                                strReportType,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }
                        else if (strReportType == "202" || strReportType == "9202")
                        {
                            nRet = Create_202_report(
                                stop,
                                strLocation,
                                time.Time,
                                strCfgFile,
                                macro_table,
                                strOutputFileName,
                                strReportType,
                                out strError);
                            if (nRet == -1)
                                return -1;
                        }
                        else if (strReportType == "212" || strReportType == "9212"
                            || strReportType == "213" || strReportType == "9213")
                        {
                            // List<string> names = StringUtil.SplitList(strNameTable);
                            List<OneClassType> class_table = null;
                            nRet = OneClassType.BuildClassTypes(strNameTable,
            out class_table,
            out strError);
                            if (nRet == -1)
                            {
                                strError = "报表类型 '" + strReportType + "' 的名字表定义不合法： " + strError;
                                return -1;
                            }

                            foreach (BiblioDbFromInfo style in class_styles)
                            {
                                if (this.InvokeRequired == false)
                                    Application.DoEvents();

#if NO
                                if (names.Count > 0)
                                {
                                    // 只处理设定的那些 class styles
                                    if (names.IndexOf(style.Style) == -1)
                                        continue;
                                }
#endif
                                OneClassType current_type = null;
                                if (class_table.Count > 0)
                                {
                                    // 只处理设定的那些 class styles
                                    int index = OneClassType.IndexOf(class_table, style.Style);
                                    if (index == -1)
                                        continue;
                                    current_type = class_table[index];
                                }

                                // 这里稍微特殊一点，循环要写入多个输出文件
                                if (string.IsNullOrEmpty(strOutputFileName) == true)
                                    strOutputFileName = Path.Combine(strOutputDir, Guid.NewGuid().ToString() + ".rml");

                                nRet = Create_212_report(
                                    stop,
                                    strLocation,
                                    style.Style,
                                    style.Caption,
                                    time.Time,
                                    strCfgFile,
                                    macro_table,
                                    current_type == null ? null : current_type.Filters,
                                    strOutputFileName,
                                    strReportType,
                                    out strError);
                                if (nRet == -1)
                                    return -1;

                                // if (nRet == 1)
                                {
                                    // 写入 index.xml
                                    nRet = WriteIndexXml(
                                        strTimeType,
                                        time.Time,
                                        GetLocationCaption(strLocation) + "-" + style.Caption,  // 把名字区别开。否则写入 <report> 会重叠覆盖
                                        strName,    // strReportsDir,
                                        strOutputFileName,
                                        strReportType,
                            nRet == 1,
                                        out strError);
                                    if (nRet == -1)
                                        return -1;

                                    strOutputFileName = "";
                                }
                            }
                        }

                        if (File.Exists(strOutputFileName) == true)
                        {
                            // 写入 index.xml
                            nRet = WriteIndexXml(
                                strTimeType,
                                time.Time,
                                GetLocationCaption(strLocation),  // 把名字区别开。否则写入 <report> 会重叠覆盖
                                strName,    // strReportsDir,
                                strOutputFileName,
                                strReportType,
                                true,
                                out strError);
                            if (nRet == -1)
                                return -1;

                            strOutputFileName = "";
                        }

                        iLocation++;
                    }
                    this.ClearMessage();
                    // TODO: 总的馆藏地点还要来一次

                } // end 2xx

#endif
                else
                {
                    /*
                    strError = "未知的 strReportType '" + strReportType + "'";
                    goto ERROR1;
                    */
                    continue;
                }

                if (string.IsNullOrEmpty(strOutputFileName) == false
                    && nAdd != 0)
                {
                    if (nAdd == 1 && File.Exists(strOutputFileName) == false)
                    {
                    }
                    else
                    {
                        // 写入 index.xml
                        nRet = WriteIndexXml(
                            fileType,
                            strTimeType,
                            time.Time,
                            strName,
                            "", // strReportsDir,
                            strOutputFileName,
                            strReportType,
                            nAdd == 1,
                            _cssTemplate,
                            out strError);
                        if (nRet == -1)
                            goto ERROR1;
                    }
                }
            }
            // }

            return new NormalResult { Value = 1 };

        ERROR1:
            return new NormalResult
            {
                Value = -1,
                ErrorInfo = strError
            };
        }

        // 获得适合用作报表名或文件名 的 地点名称字符串
        static string GetLocationCaption(string strText)
        {
            if (string.IsNullOrEmpty(strText) == true)
                return "[空]";

            if (strText[strText.Length - 1] == '/')
                return strText.Substring(0, strText.Length - 1) + "[全部]";

            return strText;
        }

        // 根据时间字符串得到 子目录名
        // 2014 --> 2014
        // 201401 --> 2014/201401
        // 20140101 --> 2014/201401/20140101
        static string GetSubDirName(string strTime)
        {
            if (string.IsNullOrEmpty(strTime) == true)
                return "";
            if (strTime.Length == 4)
                return strTime;
            if (strTime.Length == 6)
                return strTime.Substring(0, 4) + "/" + strTime;
            if (strTime.Length == 8)
                return strTime.Substring(0, 4) + "/" + strTime.Substring(0, 6) + "/" + strTime;
            return strTime;
        }

        ObjectCache<XmlDocument> _indexCache = new ObjectCache<XmlDocument>();

        // 收缩 cache 尺寸
        // parameters:
        //      bShrinkAll  是否全部收缩
        void ShrinkIndexCache(bool bShrinkAll)
        {
            if (_indexCache.Count > 10
                || bShrinkAll == true)
            {
                foreach (string filename in _indexCache.Keys)
                {
                    XmlDocument? dom = _indexCache.FindObject(filename);
                    if (dom == null)
                    {
                        Debug.Assert(false, "");
                        continue;
                    }

                    // string strHashCode = dom.GetHashCode().ToString();

                    if (IsEmpty(dom) == true)
                    {
                        // 2014/6/12
                        // 如果 DOM 为空，则要删除物理文件
                        try
                        {
                            File.Delete(filename);
                        }
                        catch (DirectoryNotFoundException)
                        {
                        }
                    }
                    else
                    {
                        try
                        {
                            dom.Save(filename);
                        }
                        catch (DirectoryNotFoundException)
                        {
                            OperLogLoader.TryCreateDir(Path.GetDirectoryName(filename));
                            dom.Save(filename);
                        }
                    }
                }

                _indexCache.Clear();
            }
        }

        void ClearCache()
        {
            this._writerCache.Clear();
            this._libraryLocationCache.Clear();
        }

        static bool IsEmpty(XmlDocument dom)
        {
            if (dom.DocumentElement.ChildNodes.Count == 0
                && dom.DocumentElement.Name != "dir"
                && dom.DocumentElement.Name != "report")
                return true;
            return false;
        }

        // 检查一个路径是文件还是目录
        static string CheckFileOrDirectory(string path)
        {
            // Check if the path is a file
            if (File.Exists(path))
            {
                return "file";
            }
            // Check if the path is a directory
            else if (Directory.Exists(path))
            {
                return "directory";
            }
            else
            {
                return "doesNotExist";
            }
        }

        // 将一个统计文件条目写入到 index.xml 的 DOM 中
        // 注：要确保每个报表的名字 strTableName 是不同的。如果同一报表要在不同条件下输出多次，需要把条件字符串也加入到名字中
        // parameters:
        //      strOutputDir    报表输出目录。例如 c:\users\administrator\dp2circulation_v2\reports
        int WriteIndexXml(
            FileType _fileType,
            string strTimeType,
            string strTime,
            string strTableName,
            // string strOutputDir,
            string strGroupName,
            string strReportFileName,
            string strReportType,
            bool bAdd,
            string cssTemplate,
            out string strError)
        {
            strError = "";

            // 这里决定在分馆所述的目录内，如何划分 index.xml 文件的层级和个数
            string strOutputDir = Path.GetDirectoryName(strReportFileName);
            string strFileName = Path.Combine(strOutputDir, "index.xml");

            XmlDocument? index_dom = _indexCache.FindObject(strFileName);
            if (index_dom == null && bAdd == false)
            {
                try
                {
                    var result = CheckFileOrDirectory(strReportFileName);
                    if (result == "directory")
                    {
                        DeleteDirectory(strReportFileName);
                    }
                    else if (result == "file")
                    {
                        File.Delete(strReportFileName);
                    }
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (Exception ex)
                {
                    throw;
                }
                return 0;
            }
            if (index_dom == null)
            {
                index_dom = new XmlDocument();
                if (File.Exists(strFileName) == true)
                {
                    try
                    {
                        index_dom.Load(strFileName);
                    }
                    catch (Exception ex)
                    {
                        strError = "装入文件 " + strFileName + " 时出错: " + ex.Message;
                        return -1;
                    }
                }
                else
                {
                    index_dom.LoadXml("<root />");
                }
                _indexCache.SetObject(strFileName, index_dom);
            }

            // string strHashCode = index_dom.GetHashCode().ToString();

            // 根据时间类型创建一个 index.xml 中的 item 元素
            XmlElement? item = null;
            if (strReportType == "131" || strReportType == "9131")
            {
                item = CreateDirNode(index_dom.DocumentElement,
                    strTableName + "-" + strReportType,
                    bAdd ? 1 : -1);
                if (bAdd == false)
                    return 0;
                Debug.Assert(item != null, "");

                string strNewFileName = "." + strReportFileName.Substring(strOutputDir.Length);
                item.SetAttribute("link", strNewFileName.Replace("\\", "/"));
            }
            else
            {
                if (string.IsNullOrEmpty(strGroupName) == false)
                {
                    item = CreateReportNode(index_dom.DocumentElement,
                        strGroupName + "-" + strReportType,
                        strTableName,
                        bAdd);
                }
                else
                {
                    item = CreateReportNode(index_dom.DocumentElement,
                        strGroupName,
                        strTableName + "-" + strReportType,
                        bAdd);
                }

                if (bAdd == false)
                {
                    // TODO: 还需要删除 index.xml 中条目指向的原有文件
                    try
                    {
                        File.Delete(strReportFileName);
                    }
                    catch (DirectoryNotFoundException)
                    {
                    }

                    return 0;
                }

                Debug.Assert(item != null, "");

                /*
                 * 文件名不能包含任何以下字符：\ / : * ?"< > |
                 * 
    对于命名的文件、 文件夹或快捷方式是有效的字符包括字母 (A-Z) 和数字 (0-9)，再加上下列特殊字符的任意组合：
       ^   Accent circumflex (caret)
       &   Ampersand
       '   Apostrophe (single quotation mark)
       @   At sign
       {   Brace left
       }   Brace right
       [   Bracket opening
       ]   Bracket closing
       ,   Comma
       $   Dollar sign
       =   Equal sign
       !   Exclamation point
       -   Hyphen
# Number sign
       (   Parenthesis opening
       )   Parenthesis closing
       %   Percent
       .   Period
       +   Plus
       ~   Tilde
       _   Underscore             * */
                string strName = item.GetAttribute("name").Replace("/", "+");

                Debug.Assert(strGroupName.IndexOf("/") == -1, "");

                Debug.Assert(strName.IndexOf("|") == -1, "");
                // 将文件名改名
                string strFileName1 = Path.Combine(Path.GetDirectoryName(strReportFileName),
                    GetValidPathString((string.IsNullOrEmpty(strGroupName) == false ? strGroupName + "-" : "") + strName)
                    + Path.GetExtension(strReportFileName));
                if (File.Exists(strFileName1) == true)
                    File.Delete(strFileName1);

#if NO
                FileAttributes attr = File.GetAttributes(strReportHtmlFileName);
#endif

                File.Move(strReportFileName, strFileName1);
                strReportFileName = strFileName1;

#if NO
                FileAttributes attr1 = File.GetAttributes(strReportHtmlFileName);
                Debug.Assert(attr == attr1, "");
#endif
                string strNewFileName = "." + strReportFileName.Substring(strOutputDir.Length);

                // 创建 .html 文件
                if ((_fileType & FileType.HTML) != 0)
                {
                    string strHtmlFileName = Path.Combine(Path.GetDirectoryName(strReportFileName), Path.GetFileNameWithoutExtension(strReportFileName) + ".html");
                    int nRet = Report.RmlToHtml(strReportFileName,
                        strHtmlFileName,
                        cssTemplate,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    RemoveArchiveAttribute(strHtmlFileName);
                }

                // 创建 Excel 文件
                if ((_fileType & FileType.Excel) != 0)
                {
                    string strExcelFileName = Path.Combine(Path.GetDirectoryName(strReportFileName), Path.GetFileNameWithoutExtension(strReportFileName) + ".xlsx");
                    int nRet = Report.RmlToExcel(strReportFileName,
                        strExcelFileName,
                        out strError);
                    if (nRet == -1)
                        return -1;
                    RemoveArchiveAttribute(strExcelFileName);
                }

                // 删除以前的对照关系
                string strOldFileName = item.GetAttribute("link");
                if (string.IsNullOrEmpty(strOldFileName) == false)
                // && PathUtil.IsEqual(strOldFileName, strHtmlFileName) == false
                {
                    string strOldPhysicalPath = GetRealPath(strOutputDir,
        strOldFileName);

                    if (IsPathEqual(strOldPhysicalPath, strReportFileName) == false
                    && File.Exists(strOldPhysicalPath) == true)
                    {
                        try
                        {

                            File.Delete(strOldPhysicalPath);
                        }
                        catch
                        {
                        }
                    }
                }

                item.SetAttribute("link", strNewFileName.Replace("\\", "/"));
#if DEBUG
                // 检查文件是否存在
                if (File.Exists(strReportFileName) == false)
                {
                    strError = strTime + " " + strTableName + " 的文件 '" + strReportFileName + "' 不存在";
                    return -1;
                }
#endif
            }

            // _indexDom.Save(strFileName);
            ShrinkIndexCache(false);
#if NO
#if DEBUG
            FileAttributes attr1 = File.GetAttributes(strFileName);
            Debug.Assert((attr1 & FileAttributes.Archive) == FileAttributes.Archive, "");
#endif
#endif

            // File.SetAttributes(strFileName, FileAttributes.Archive);
            return 0;
        }

        // TODO: 需要用缓存优化
        // 将一个统计文件条目写入到 131 子目录中的 index.xml 的 DOM 中
        // parameters:
        //      strOutputDir    index.xml 所在目录
        int Write_131_IndexXml(
            FileType fileType,
            string strDepartment,
            string strPersonName,
            string strPatronBarcode,
            string strOutputDir,
            string strReportFileName,
            string strReportType,
            string _cssTemplate,
            out string strError)
        {
            strError = "";

            string strFileName = Path.Combine(strOutputDir, "index.xml");

            XmlDocument? index_dom = this._indexCache.FindObject(strFileName);
            if (index_dom == null)
            {
                index_dom = new XmlDocument();
                if (File.Exists(strFileName) == true)
                {
                    try
                    {
                        index_dom.Load(strFileName);
                    }
                    catch (Exception ex)
                    {
                        strError = "装入文件 " + strFileName + " 时出错: " + ex.Message;
                        return -1;
                    }
                }
                else
                {
                    index_dom.LoadXml("<root />");
                }
                this._indexCache.SetObject(strFileName, index_dom);
            }

            // 创建一个 index.xml 中的 item 元素
            var item = Create_131_ItemNode(index_dom.DocumentElement,
                strDepartment,
                strPersonName + "-" + strPatronBarcode);
            Debug.Assert(item != null, "");
            item.SetAttribute("type", strReportType);

            string strNewFileName = "." + strReportFileName.Substring(strOutputDir.Length);

            // 创建 .html 文件
            if ((fileType & FileType.HTML) != 0)
            {
                string strHtmlFileName = Path.Combine(Path.GetDirectoryName(strReportFileName), Path.GetFileNameWithoutExtension(strReportFileName) + ".html");
                int nRet = Report.RmlToHtml(strReportFileName,
                    strHtmlFileName,
                    _cssTemplate,
                    out strError);
                if (nRet == -1)
                    return -1;
                RemoveArchiveAttribute(strHtmlFileName);
            }

            // 创建 Excel 文件
            if ((fileType & FileType.Excel) != 0)
            {
                string strExcelFileName = Path.Combine(Path.GetDirectoryName(strReportFileName), Path.GetFileNameWithoutExtension(strReportFileName) + ".xlsx");
                int nRet = Report.RmlToExcel(strReportFileName,
                    strExcelFileName,
                    out strError);
                if (nRet == -1)
                    return -1;
                RemoveArchiveAttribute(strExcelFileName);
            }

            // 删除以前的对照关系
            string strOldFileName = item.GetAttribute("link");
            if (string.IsNullOrEmpty(strOldFileName) == false)
            // && PathUtil.IsEqual(strOldFileName, strHtmlFileName) == false
            {
                string strOldPhysicalPath = GetRealPath(strOutputDir,
    strOldFileName);

                if (IsPathEqual(strOldPhysicalPath, strReportFileName) == false
                && File.Exists(strOldPhysicalPath) == true)
                {
                    try
                    {

                        File.Delete(strOldPhysicalPath);
                    }
                    catch
                    {
                    }
                }
            }

            item.SetAttribute("link", strNewFileName.Replace("\\", "/"));
#if DEBUG
            // 检查文件是否存在
            if (File.Exists(strReportFileName) == false)
            {
                strError = "文件 '" + strReportFileName + "' 不存在";
                return -1;
            }
#endif

            // index_dom.Save(strFileName);
            ShrinkIndexCache(false);

            // 131 目录的 index.html 不要上传
            // File.SetAttributes(strFileName, FileAttributes.Archive);
            return 0;
        }

        // 创建一个 131 类型的 index.xml 中的 item 元素
        static XmlElement Create_131_ItemNode(XmlElement root,
            string strDepartment,
            string strReportName)
        {
            XmlElement department = null;

            if (string.IsNullOrEmpty(strDepartment) == false)
            {
                department = root.SelectSingleNode("dir[@name='" + strDepartment + "']") as XmlElement;
                if (department == null)
                {
                    department = root.OwnerDocument.CreateElement("dir");
                    root.AppendChild(department);
                    department.SetAttribute("name", strDepartment);
                }
            }
            else
            {
                department = root;
                // strDepartment 如果为空,则不用创建 dir 元素了
            }

            var item = department.SelectSingleNode("report[@name='" + strReportName + "']") as XmlElement;
            if (item == null)
            {
                item = department.OwnerDocument.CreateElement("report");
                department.AppendChild(item);
                item.SetAttribute("name", strReportName);
            }
            return item;
        }


        // 测试strPath1是否和strPath2为同一文件或目录
        public static bool IsPathEqual(string strPath1, string strPath2)
        {
            if (String.IsNullOrEmpty(strPath1) == true
                && String.IsNullOrEmpty(strPath2) == true)
                return true;

            if (String.IsNullOrEmpty(strPath1) == true)
                return false;

            if (String.IsNullOrEmpty(strPath2) == true)
                return false;

            if (strPath1 == strPath2)
                return true;

            // TODO: new DirecotryInfo() 对一个文件操作时候会怎样？会抛出异常么? 需要测试一下 2016/11/6
            FileSystemInfo fi1 = new DirectoryInfo(strPath1);
            FileSystemInfo fi2 = new DirectoryInfo(strPath2);

            string strNewPath1 = fi1.FullName.ToUpper();
            string strNewPath2 = fi2.FullName.ToUpper();

            if (strNewPath1.Length != 0)
            {
                if (strNewPath1[strNewPath1.Length - 1] != '\\')
                    strNewPath1 += "\\";
            }
            if (strNewPath2.Length != 0)
            {
                if (strNewPath2[strNewPath2.Length - 1] != '\\')
                    strNewPath2 += "\\";
            }

            if (strNewPath1.Length != strNewPath2.Length)
                return false;

            if (strNewPath1 == strNewPath2)
                return true;

            return false;
        }

        public static void DeleteDirectory(string strDirPath)
        {
            try
            {
                Directory.Delete(strDirPath, true);
            }
            catch (DirectoryNotFoundException)
            {
                // 不存在就算了
            }
            catch (IOException)
            {
                Thread.Sleep(0);
                Directory.Delete(strDirPath, true);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(0);
                Directory.Delete(strDirPath, true);
            }
        }


        // parameters:
        //      nAdd    -1: delete 0: detect  1: add
        static XmlElement? CreateDirNode(XmlElement root,
            string strDirName,
            int nAdd)
        {
            var dir = root.SelectSingleNode("dir[@name='" + strDirName + "']") as XmlElement;
            if (nAdd == 0)
                return dir;
            if (nAdd == -1)
            {
                if (dir != null)
                    dir.ParentNode?.RemoveChild(dir);
                return dir;
            }
            Debug.Assert(nAdd == 1, "");
            if (dir == null)
            {
                dir = root.OwnerDocument.CreateElement("dir");
                root.AppendChild(dir);
                dir.SetAttribute("name", strDirName);
            }

            return dir;
        }

        // 创建一个 index.xml 中的 report 元素
        static XmlElement? CreateReportNode(XmlElement root,
            string strGroupName,
            string strReportName,
            bool bAdd)
        {
            var start = root;
            if (string.IsNullOrEmpty(strGroupName) == false)
            {
                XmlElement? group = null;
                if (bAdd == false)
                {
                    group = CreateDirNode(root, strGroupName, 0);
                    if (group == null)
                        return null;
                }
                else
                {
                    group = CreateDirNode(root, strGroupName, 1);
                    group?.SetAttribute("type", "group");
                }
                start = group;
            }

            var item = start?.SelectSingleNode("report[@name='" + strReportName + "']") as XmlElement;
            if (bAdd == false)
            {
                if (item != null)
                    item.ParentNode?.RemoveChild(item);
                return item;
            }
            if (item == null)
            {
                item = start?.OwnerDocument.CreateElement("report");
                start?.AppendChild(item);
                item?.SetAttribute("name", strReportName);
            }

            return item;
        }

        public static void RemoveArchiveAttribute(string strFileName)
        {
            // File.SetAttributes(strFileName, FileAttributes.Normal);
        }

        // 获得文件的物理路径
        // parameters:
        //      strFileName 为 "./2013/some" 或者 "c"\\dir\、file" 这样的 
        static string GetRealPath(string strOutputDir,
            string strFileName)
        {

            if (StringUtil.HasHead(strFileName, "./") == true
                || StringUtil.HasHead(strFileName, ".\\") == true)
            {
                return strOutputDir + strFileName.Substring(1);
            }

            return strFileName;
        }

        ObjectCache<ReportWriter> _writerCache = new ObjectCache<ReportWriter>();

        int GetReportWriter(string strCfgFile,
            out ReportWriter? writer,
            out string strError)
        {
            strError = "";

            writer = _writerCache.FindObject(strCfgFile);
            if (writer == null)
            {
                writer = new ReportWriter();
                int nRet = writer.Initial(strCfgFile, out strError);
                if (nRet == -1)
                    return -1;
                _writerCache.SetObject(strCfgFile, writer);
            }

            return 0;
        }



        // 根据 index.xml 文件创建 index.html 文件
        // return:
        //      -1  出错
        //      0   没有创建。因为 index.xml 文件不存在
        //      1   创建成功
        static int CreateIndexHtmlFile(
            FileType _fileType,
            string strIndexXmlFileName,
            string strIndexHtmlFileName,
            out string strError)
        {
            strError = "";

            if (File.Exists(strIndexXmlFileName) == false)
                return 0;

            XmlDocument dom = new XmlDocument();
            try
            {
                dom.Load(strIndexXmlFileName);
            }
            catch (Exception ex)
            {
                strError = "装入文件 " + strIndexXmlFileName + " 时出错: " + ex.Message;
                return -1;
            }


            StringBuilder text = new StringBuilder(4096);

            text.Append("<html><body>");

            // TODO: 也可以用 <dir> 元素的上级
            OutputHtml(_fileType, dom.DocumentElement, text);

            text.Append("</body></html>");

            WriteToOutputFile(strIndexHtmlFileName,
                text.ToString(),
                Encoding.UTF8);
            // index.html 不要上传
            // File.SetAttributes(strIndexHtmlFileName, FileAttributes.Archive);
            return 1;
        }

        static public void WriteToOutputFile(string strFileName,
    string strText,
    Encoding encoding)
        {
            using StreamWriter sw = new StreamWriter(strFileName,
                false,	// append
                encoding);
            sw.Write(strText);
        }

        static void OutputHtml(
            FileType _fileType,
    XmlElement dir,
    StringBuilder text)
        {

            {
                string strLink = dir.GetAttribute("link");
                string strDirName = GetDisplayTimeString(dir.GetAttribute("name"));
                if (string.IsNullOrEmpty(strLink) == true)
                {
                    text.Append("<div>");
                    text.Append(HttpUtility.HtmlEncode(strDirName));
                    text.Append("</div>");
                }
                else
                {
                    text.Append("<li>");
                    text.Append("<a href='" + strLink + "' >");
                    text.Append(HttpUtility.HtmlEncode(strDirName) + " ...");
                    text.Append("</a>");
                    text.Append("</li>");
                    return;
                }
            }

            text.Append("<ul>");

            var reports = dir.SelectNodes("report");
            foreach (XmlElement report in reports)
            {
                string strName = report.GetAttribute("name");
                string strLink = report.GetAttribute("link");

                // link 加工为 .html 形态
                if ((_fileType & FileType.HTML) != 0)
                {
                    strLink = Path.Combine(Path.GetDirectoryName(strLink), Path.GetFileNameWithoutExtension(strLink) + ".html");
                }

                text.Append("<li>");

                text.Append("<a href='" + strLink + "' >");
                text.Append(HttpUtility.HtmlEncode(strName));
                text.Append("</a>");

                text.Append("</li>");
            }

            var dirs = dir.SelectNodes("dir");
            foreach (XmlElement sub_dir in dirs)
            {
                OutputHtml(_fileType, sub_dir, text);
            }

            text.Append("</ul>");
        }

        static string GetDisplayTimeString(string strTime)
        {
            if (strTime.Length == 8)
                return strTime.Substring(0, 4) + "." + strTime.Substring(4, 2) + "." + strTime.Substring(6, 2);
            if (strTime.Length == 6)
                return strTime.Substring(0, 4) + "." + strTime.Substring(4, 2);

            return strTime;
        }

        // 获得一个日期的下一天
        // parameters:
        //      strDate 8字符的时间格式
        static string GetNextDate(string strDate)
        {
            if (string.IsNullOrEmpty(strDate))
                return "";
            DateTime start;
            try
            {
                start = DateTimeUtil.Long8ToDateTime(strDate);
            }
            catch
            {
                return strDate; // 返回原样的字符串
            }

            return DateTimeUtil.DateTimeToString8(start + new TimeSpan(1, 0, 0, 0, 0));
        }


        // 特定分馆的报表输出目录
        static string GetReportOutputDir(
            string strBaseDirectory,
            string strLibraryCode)
        {
            // 将创建好的报表文件存储在和每个 dp2library 服务器和用户名相关的目录中
            return Path.Combine(strBaseDirectory, "reports\\" + GetValidPathString(GetDisplayLibraryCode(strLibraryCode)));
        }

        #region GetValidPathString()

        // 附加的一些文件名非法字符。比如 XP 下 Path.GetInvalidPathChars() 不知何故会遗漏 '*'
        static string spec_invalid_chars = "*?:";

        public static string GetValidPathString(string strText, string strReplaceChar = "_")
        {
            if (string.IsNullOrEmpty(strText) == true)
                return "";

            char[] invalid_chars = Path.GetInvalidPathChars();
            StringBuilder result = new StringBuilder();
            foreach (char c in strText)
            {
                if (c == ' ')
                    continue;
                if (IndexOf(invalid_chars, c) != -1
                    || spec_invalid_chars.IndexOf(c) != -1)
                    result.Append(strReplaceChar);
                else
                    result.Append(c);
            }

            return result.ToString();
        }

        static int IndexOf(char[] chars, char c)
        {
            int i = 0;
            foreach (char c1 in chars)
            {
                if (c1 == c)
                    return i;
                i++;
            }

            return -1;
        }

        public static string GetValidDepartmentString(string strText, string strReplaceChar = "_")
        {
            if (strText != null)
                strText = strText.Trim();
            if (string.IsNullOrEmpty(strText) == true)
                return "其他部门";

            // 文件名非法字符
            string department_invalid_chars = " /";

            StringBuilder result = new StringBuilder();
            foreach (char c in strText)
            {
                if (c == ' ')
                    continue;
                if (department_invalid_chars.IndexOf(c) != -1)
                    result.Append(strReplaceChar);
                else
                    result.Append(c);
            }

            return result.ToString();
        }

        #endregion

        // 获得显示用的馆代码形态
        public static string GetDisplayLibraryCode(string strLibraryCode)
        {
            if (string.IsNullOrEmpty(strLibraryCode) == true)
                return "[全局]";
            return strLibraryCode;
        }

        // 获得上个月的 4 字符时间
        static string GetPrevMonthString(DateTime current)
        {
            if (current.Month == 1)
                return (current.Year - 1).ToString().PadLeft(4, '0') + "12";
            return current.Year.ToString().PadLeft(4, '0') + (current.Month - 1).ToString().PadLeft(2, '0');
        }

        // parameters:
        //      strType 时间单位类型。 year month week day 之一
        //      strDateRange 日期范围。其中结束日期不允许超过今天。因为今天的日志可能没有同步完
        //      bDetect 是否要增加一个探测时间值? 根据开始的日期，如果属于每月一号则负责探测上一个月； 如果属于 1 月 1 号则负责探测上一年
        //      strRealEndDate  返回实际处理完的最后一天
        static int GetTimePoints(
            string strDailyEndDate, // 最近一次每日同步的最后日期
            string strType,
            string strDateRange,
            bool bDetect,
            out string strRealEndDate,
            out List<OneTime> values,
            out string strError)
        {
            strError = "";
            values = new List<OneTime>();
            strRealEndDate = "";

            string strStartDate = "";
            string strEndDate = "";

            try
            {
                // 将日期字符串解析为起止范围日期
                // throw:
                //      Exception
                DateTimeUtil.ParseDateRange(strDateRange,
                    out strStartDate,
                    out strEndDate);

                // 2014/3/19
                if (string.IsNullOrEmpty(strEndDate) == true)
                    strEndDate = strStartDate;
            }
            catch (Exception)
            {
                strError = "日期范围字符串 '" + strDateRange + "' 格式不正确";
                return -1;
            }

            DateTime start;
            try
            {
                start = DateTimeUtil.Long8ToDateTime(strStartDate);
            }
            catch
            {
                strError = "统计日期范围 '" + strDateRange + "' 中的开始日期 '" + strStartDate + "' 不合法。应该是 8 字符的日期格式";
                return -1;
            }

            DateTime end;
            try
            {
                end = DateTimeUtil.Long8ToDateTime(strEndDate);
            }
            catch
            {
                strError = "统计日期范围 '" + strDateRange + "' 中的结束日期 '" + strEndDate + "' 不合法。应该是 8 字符的日期格式";
                return -1;
            }

            /*
            string strDailyEndDate = "";
            {
                long lIndex = 0;
                string strState = "";

                // 读入断点信息
                // return:
                //      -1  出错
                //      0   正常
                //      1   首次创建尚未完成
                int nRet = LoadDailyBreakPoint(
                    out strDailyEndDate,
                    out lIndex,
                    out strState,
                    out strError);
                if (nRet == -1)
                {
                    strError = "获得日志同步最后日期时出错: " + strError;
                    return -1;
                }
                if (nRet == 1)
                {
                    strError = "首次创建本地存储尚未完成，无法创建报表";
                    return -1;
                }
            }
            */

            DateTime daily_end;
            try
            {
                daily_end = DateTimeUtil.Long8ToDateTime(strDailyEndDate);
            }
            catch
            {
                strError = "日志同步最后日期 '" + strDailyEndDate + "' 不合法。应该是 8 字符的日期格式";
                return -1;
            }

            // 两个日期都不允许超过今天
            if (start >= daily_end)
            {
                strError = "统计时间范围的起点不应晚于 日志同步最后日期 " + strDailyEndDate + " 的前一天";
                return -1;
            }

            if (end >= daily_end)
                end = daily_end - new TimeSpan(1, 0, 0, 0, 0);

            strRealEndDate = DateTimeUtil.DateTimeToString8(end);

            DateTime end_plus_one = end + new TimeSpan(1, 0, 0, 0, 0);

            if (strType == "free")
            {
                {
                    OneTime time = new OneTime(DateTimeUtil.DateTimeToString8(start) + "-" + DateTimeUtil.DateTimeToString8(end));
                    values.Add(time);
                }
            }
            else if (strType == "year")
            {
                int nFirstYear = start.Year;

                if (bDetect == true
                    && start.Month == 1 && start.Day == 1)
                {
                    // 每年 1 月 1 号，负责探测上一年
                    OneTime time = new OneTime((start.Year - 1).ToString().PadLeft(4, '0'), true);
                    values.Add(time);
                }

                int nEndYear = end_plus_one.Year;
                for (int nYear = nFirstYear; nYear < nEndYear; nYear++)
                {
                    OneTime time = new OneTime(nYear.ToString().PadLeft(4, '0'));
                    values.Add(time);
                }
            }
            else if (strType == "month")
            {
                if (bDetect == true
    && start.Month == 1)
                {
                    // 每月 1 号，负责探测上个月
                    OneTime time = new OneTime(GetPrevMonthString(start), true);
                    values.Add(time);
                }

                DateTime current = new DateTime(start.Year, start.Month, 1);
                DateTime end_month = new DateTime(end_plus_one.Year, end_plus_one.Month, 1);
                while (current < end_month)
                {
                    values.Add(new OneTime(current.Year.ToString().PadLeft(4, '0') + current.Month.ToString().PadLeft(2, '0')));
                    // 下一个月
                    if (current.Month >= 12)
                        current = new DateTime(current.Year + 1, 1, 1);
                    else
                        current = new DateTime(current.Year, current.Month + 1, 1);
                }
            }
            else if (strType == "day")
            {
                DateTime current = new DateTime(start.Year, start.Month, start.Day);
                while (current <= end)
                {
                    values.Add(new OneTime(current.Year.ToString().PadLeft(4, '0')
                        + current.Month.ToString().PadLeft(2, '0')
                        + current.Day.ToString().PadLeft(2, '0')));
                    // 下一天
                    current += new TimeSpan(1, 0, 0, 0);
                }
            }
            else if (strType == "week")
            {
                strError = "暂不支持 week";
                return -1;
            }

            return 0;
        }



        public class PatronInfo
        {
            public string? Barcode { get; set; }
            public string? Name { get; set; }
            public string? Department { get; set; }

            // 用于调试观察
            public string? RecPath { get; set; }
        }

        // 获得一个分馆内读者记录的证条码号、姓名和单位名称
        // parameters:
        //      strLibraryCode  馆代码。如果为 "*" 表示希望获得所有分馆的读者记录
        public static List<PatronInfo> GetPatronInfo(
            LibraryContext context,
            string strLibraryCode)
        {
            return context.Patrons
                .Where(o => (strLibraryCode == "*" || o.LibraryCode == strLibraryCode) && string.IsNullOrEmpty(o.Barcode) == false)
                .Select(o => new PatronInfo
                {
                    Barcode = o.Barcode,
                    Name = o.Name,
                    Department = o.Department,
                    RecPath = o.RecPath,
                })
                .OrderBy(o => o.Department).ThenBy(o => o.Barcode)
                .ToList();
        }

        // 创建一个分馆的全部读者的借阅清单
        // 在输出目录下，以读者单位名创建子目录，然后在其内创建报表文件
        // parameters:
        //      param_table 要求 libraryCode 参数
        //                  注: 不要求 patronBarcode 参数
        //      strOutputDir    输出目录
        // return:
        //      -1  出错
        //      0   没有创建目录
        //      >=1   创建了目录
        public int BuildAllReport_131(LibraryContext context,
Hashtable param_table,
string strCfgFile,
//string strStartDate,
//string strEndDate,
//ReportWriter writer,
//Hashtable macro_table,
string strOutputDir,
string strReportType,
FileType fileType,
string cssTemplate,
CancellationToken token,
out string strError)
        {
            strError = "";

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

            var strLibraryCode = param_table["libraryCode"] as string;
            if (strLibraryCode == null)
                throw new ArgumentException("param_table 中必须包含 libraryCode 参数");

            var patrons = GetPatronInfo(
    context,
    strLibraryCode);


            // 删除输出目录内所有子目录和文件
            if (string.IsNullOrEmpty(strOutputDir) == false)
            {
                // 不能使用根目录
                string strRoot = Directory.GetDirectoryRoot(strOutputDir);
                if (IsPathEqual(strRoot, strOutputDir) == true)
                {
                }
                else
                    DeleteDirectory(strOutputDir);
            }

            if (patrons.Count == 0)
            {
                // 下级全部目录此时已经删除
                return 0;
            }

            // int count = 0;
            int nWriteCount = 0;
            foreach (var patron in patrons)
            {
                var strDepartmentName = GetValidDepartmentString(patron.Department);

                string strPureFileName = GetValidPathString(strDepartmentName) + "\\" + GetValidPathString(patron.Barcode + "_" + patron.Name) + ".rml";
                string strOutputFileName = "";

                try
                {
                    strOutputFileName = Path.Combine(strOutputDir,
                         // strLibraryCode + "\\" + 
                         strPureFileName);    // xlsx
                }
                catch (System.ArgumentException ex)
                {
                    strError = ("文件名字符串 '" + strPureFileName + "' 中有非法字符。" + ex.Message);
                    return -1;
                }

                param_table["patronBarcode"] = patron.Barcode;

                macro_table["%name%"] = patron.Name;
                macro_table["%department%"] = patron.Department;

                int nRet = GetReportWriter(strCfgFile,
    out ReportWriter writer,
    out strError);
                if (nRet == -1)
                    return -1;

                nRet = Report.BuildReport_131(context,
param_table,
strStartDate,
strEndDate,
writer,
macro_table,
strOutputFileName);

                // TODO: 没有数据的读者，是否在 index.xml 也创建一个条目?
                if (nRet >= 1)
                {
                    // 将一个统计文件条目写入到 131 子目录中的 index.xml 的 DOM 中
                    // parameters:
                    //      strOutputDir    index.xml 所在目录
                    nRet = Write_131_IndexXml(
                        fileType,
                        strDepartmentName,
                        patron.Name,
                        patron.Barcode,
                        strOutputDir,
                        strOutputFileName,
                        strReportType,
                        cssTemplate,
                        out strError);
                    if (nRet == -1)
                        return -1;

                    nWriteCount++;
                }

                // count += nRet;
            }

            if (nWriteCount > 0
    && (fileType & FileType.HTML) != 0)
            {
                string strIndexXmlFileName = Path.Combine(strOutputDir, "index.xml");
                string strIndexHtmlFileName = Path.Combine(strOutputDir, "index.html");

                //if (stop != null)
                //    stop?.SetMessage("正在创建 " + strIndexHtmlFileName);

                // 根据 index.xml 文件创建 index.html 文件
                int nRet = CreateIndexHtmlFile(
                    fileType,
                    strIndexXmlFileName,
                    strIndexHtmlFileName,
                    out strError);
                if (nRet == -1)
                    return -1;
            }

            return nWriteCount;
        }


        #region Location Cache

        // 分馆的全部馆藏地点 cache
        ObjectCache<List<string>> _libraryLocationCache = new ObjectCache<List<string>>();

        List<string> GetLibraryLocation(
            string strLibraryCode)
        {
            // 获得分馆的所有馆藏地点
            var locations = this._libraryLocationCache.FindObject(strLibraryCode);
            if (locations == null)
            {
                locations = GetAllItemLocations(
                    strLibraryCode,
                    true);
                this._libraryLocationCache.SetObject(strLibraryCode, locations);
            }

            return locations;
        }

        // 获得一个分馆内册记录的所有馆藏地点名称
        // parameters:
        //      bRoot   是否包含分馆这个名称。如果 == true，表示要包含，在 results 中会返回一个这样的 "望湖小学/"
        public List<string> GetAllItemLocations(
            string strLibraryCode,
            bool bRoot)
        {
            using (var context = new LibraryContext(this.DatabaseConfig))
            {
                // IQueryable<DigitalPlatform.LibraryServer.Reporting.Item> query = null;
                IQueryable<DigitalPlatform.LibraryServer.Reporting.Item> query = null;

                if (string.IsNullOrEmpty(strLibraryCode) == true)
                    query = context.Items.Where(o => o.Location == null || o.Location.StartsWith("/") || EF.Functions.Like(o.Location, "%/%") == false);
                else
                    query = context.Items.Where(o => o.Location != null && EF.Functions.Like(o.Location, strLibraryCode + "/%"));
                /*
                var results = query.GroupBy(o => o.Location)
                    .Select(g => new
                    {
                        location = g.Key,
                        count = g.Count()
                    });
                */
                var results0 = query.Select(o => o.Location).Distinct();
                // .Select(g => g.Key);

                /*
                string strLibraryCodeFilter = "";
                if (string.IsNullOrEmpty(strLibraryCode) == true)
                {
                    strLibraryCodeFilter = "(location like '/%' OR location not like '%/%') ";
                }
                else
                {
                    strLibraryCodeFilter = "location like '" + strLibraryCode + "/%' ";
                }
                */

                /*
                string strCommand = "select location, count(*) as count "
             + " FROM item "
             + " WHERE " + strLibraryCodeFilter + " "
             + " GROUP BY location ;";
                */


                // 去掉字符串中的 #reservation 部分
                var results = results0.Select(o => StringUtil.GetPureLocation(o)).ToList();

                // 去重
                // StringUtil.RemoveDupNoSort(ref results);

                if (bRoot == true)
                {
                    // 一般分馆是 "海淀分馆/"。
                    // 如果遇到 dp2LibraryCode 为 ""，代表 [全局]，这样最后加入的条目就是 "/"。记住这个代表希望列出全局的所有馆藏地，即，所有不带 / 的馆藏地名字
                    string strRoot = strLibraryCode + "/";
                    if (results.IndexOf(strRoot) == -1)
                        results.Insert(0, strRoot);
                }

                return results;
            }
        }


        #endregion
    }

    // 一个处理时间
    class OneTime
    {
        public string Time = "";
        public bool Detect = false; // 是否要探测这个时间已经做过? true 表示要探测。false 表示无论如何都要做

        public OneTime()
        {
        }

        public OneTime(string strTime)
        {
            this.Time = strTime;
        }

        public OneTime(string strTime, bool bDetect)
        {
            this.Time = strTime;
            this.Detect = bDetect;
        }

        public override string ToString()
        {
            return this.Time + "|" + (this.Detect == true ? "true" : "false");
        }

        public static OneTime FromString(string strText)
        {
            string strTime = "";
            string strDetect = "";
            StringUtil.ParseTwoPart(strText,
                "|",
                out strTime,
                out strDetect);
            OneTime result = new OneTime();
            result.Time = strTime;
            result.Detect = ElementExtension.IsBooleanTrue(strDetect);

            return result;
        }

        public static string TimesToString(List<OneTime> times)
        {
            if (times == null)
                return "";

            StringBuilder text = new StringBuilder();
            foreach (OneTime time in times)
            {
                if (text.Length > 0)
                    text.Append(",");
                text.Append(time.ToString());
            }

            return text.ToString();
        }

        public static List<OneTime> TimesFromString(string strText)
        {
            List<OneTime> results = new List<OneTime>();
            if (string.IsNullOrEmpty(strText) == true)
                return results;

            string[] segments = strText.Split(new char[] { ',' });
            foreach (string strTime in segments)
            {
                results.Add(OneTime.FromString(strTime));
            }

            return results;
        }
    }

}
