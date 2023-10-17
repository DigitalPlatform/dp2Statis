using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using DigitalPlatform;
using DigitalPlatform.IO;
using DigitalPlatform.Text;
using DigitalPlatform.Xml;
using DocumentFormat.OpenXml.Bibliography;

namespace dp2Statis.Reporting
{
    /// <summary>
    /// 报表配置参数
    /// 配置了每个分馆的报表概况
    /// </summary>
    public class ReportConfigBuilder
    {
        // report_def 文件夹路径。这是一个默认的各类型报表定义文件的存储目录
        public string? ReportDefDirectory { get; set; }

        public string CfgFileName = ""; // XML 配置文件全路径

        XmlDocument? CfgDom = null;      // XML 配置文件的 DOM

        public bool Changed
        {
            get;
            set;
        }

        #region

        // 2023/9/13
        public static ReportConfigBuilder Load(string strBaseDir,
            string strFileName,
            string strReportDefDirectory)
        {
            ReportConfigBuilder builder = new ReportConfigBuilder();
            var ret = builder.LoadCfgFile(strBaseDir,
                strFileName,
                strReportDefDirectory,
                out string strError);
            if (ret == -1)
                throw new Exception(strError);
            return builder;
        }

        // parameters:
        //      style   风格。"display" 表示用于显示，其中会有“[全局]”。否则就理解为用于内部处理，这时全局就是 ""
        public ICollection<string> GetDailyReportDefLibraryCodeList(string style)
        {
            var dom = this.CfgDom;
            var nodes = dom?.DocumentElement?.SelectNodes("library/@code");
            if (nodes == null)
                return new List<string>();
            var results = nodes.Cast<XmlNode>().Select(o => o.Value).ToList().Cast<string>();
            if (results == null)
                return new List<string>();

            bool display = StringUtil.IsInList("display", style);

            var results1 = new List<string>();
            foreach(var s in results)
            {
                if (s == "[全局]" && display == false)
                    results1.Add("");
                else if (s == "" && display == true)
                    results1.Add("[全局]");
                else
                    results1.Add(s);
            }

            return results1;
        }

        public ICollection<ReportDef>? GetReportDefs(string libraryCode)
        {
            // 转换为内部形态
            libraryCode = ToKernel(libraryCode);

            var dom = this.CfgDom;
            var reports = dom?.DocumentElement?.SelectNodes($"library[@code='{libraryCode}']/reports/report");
            if (reports == null)
                return null;
            var results = new List<ReportDef>();
            foreach (XmlElement report in reports)
            {
                /*
      <report name="部门的借阅排行"
                frequency="year,month,day" 
                type="101" 
                cfgFile="C:\Users\xietao\dp2Circulation_v2\report_def\101.xml" 
                nameTable="" />                 * */
                var reportDef = new ReportDef();
                reportDef.Name = report.GetAttribute("name");
                reportDef.Frequency = report.GetAttribute("frequency");
                reportDef.Type = report.GetAttribute("type");
                reportDef.CfgFile = report.GetAttribute("cfgFile");
                reportDef.NameTable = report.GetAttribute("nameTable");
                results.Add(reportDef);
            }

            // 按照类型大小排序
            results.Sort((a, b) =>
            {
                return string.CompareOrdinal(a.Type, b.Type);
            });
            return results;
        }

        // 确保馆代码为内部形态
        public static string ToKernel(string libraryCode)
        {
            if (libraryCode == "[全局]")
                return "";
            return libraryCode;
        }

        // 确保馆代码为显示形态
        public static string ToDisplay(string libraryCode)
        {
            if (string.IsNullOrEmpty(libraryCode))
                return "[全局]";
            return libraryCode;
        }

        // 新增一个 library 元素
        // parameters:
        //      libraryCode 要添加报表定义的馆代码。可能为 "[全局]"，要变换为 "" 再创建定义
        public NormalResult AddDailyReportLibraryCode(string libraryCode)
        {
            // TODO: 检查 libraryCode 值是否合法，是否在 dp2library 实例中已经定义的馆代码之列

            // 转换为内部形态
            libraryCode = ToKernel(libraryCode);

            // 创建一个新的 <library> 元素。要对 code 属性进行查重
            // parameters:
            //      -1  出错
            //      0   成功
            //      1   已经有这个 code 属性的元素了
            var ret = CreateNewLibraryNode(libraryCode,
                out XmlElement? node,
                out string strError);
            if (ret == -1)
                return new NormalResult
                {
                    Value = -1,
                    ErrorInfo = strError
                };
            if (ret == 1)
                return new NormalResult
                {
                    Value = -1,
                    ErrorInfo = strError,
                    ErrorCode = "alreadyExist"
                };

            if (this.Changed == true)
                this.Save();
            return new NormalResult();
        }


        // 删除一个 library 元素
        public NormalResult DeleteDailyReportLibraryCode(string libraryCode)
        {
            // 转换为内部形态
            libraryCode = ToKernel(libraryCode);

            var dom = this.CfgDom;
            var library_node = dom?.DocumentElement?.SelectSingleNode($"library[@code='{libraryCode}']") as XmlElement;
            if (library_node == null)
                return new NormalResult
                {
                    Value = -1,
                    ErrorInfo = $"馆代码 '{libraryCode}' 在配置文件中没有找到"
                };

            library_node.ParentNode?.RemoveChild(library_node);
            this.Changed = true;
            this.Save();
            return new NormalResult();
        }

        // 新增一个 report 元素
        public NormalResult AddDailyReportNewDef(string libraryCode,
            string reportType)
        {
            // 转换为内部形态
            libraryCode = ToKernel(libraryCode);

            var dom = this.CfgDom;
            var library_node = dom?.DocumentElement?.SelectSingleNode($"library[@code='{libraryCode}']") as XmlElement;
            if (library_node == null)
                return new NormalResult
                {
                    Value = -1,
                    ErrorInfo = $"馆代码 '{libraryCode}' 在配置文件中没有找到"
                };

            var reports_node = DocumentExtension.CreateElementByPath(library_node, "reports");

            // 对 report/@type 进行查重
            var exists = reports_node.SelectNodes($"report[@type='{reportType}']");
            if (exists.Count > 0)
                return new NormalResult
                {
                    Value = -1,
                    ErrorInfo = $"馆代码 '{libraryCode}' 类型为 '{reportType}' 的报表定义已经在配置文件中存在，无法重复添加",
                    ErrorCode = "alreadyExists"
                };

            // 检查 reportType 对应的配置文件是否已经存在
            if (ExistsReportXmlFile(reportType) == false)
            {
                return new NormalResult
                {
                    Value = -1,
                    ErrorInfo = "尚未初始化 ReportDefDirectory 成员，无法获得报表定义文件"
                };
            }

            var report_node = dom?.CreateElement("report");
            reports_node.AppendChild(report_node);

            /*
      <report name="部门的借阅排行" 
            frequency="year,month,day" 
            type="101" 
            cfgFile="C:\Users\xietao\dp2Circulation_v2\report_def\101.xml" 
            nameTable="" />
            * */
            // 根据 type 查到 name
            report_node.SetAttribute("name", GetTypeName(reportType));
            report_node.SetAttribute("frequency", "year,month,day");
            report_node.SetAttribute("type", reportType);
            report_node.SetAttribute("cfgFile", GetReportXmlFilePath(reportType));

            this.Changed = true;
            this.Save();
            return new NormalResult();
        }

        // UpdateDailyReportDef
        // 更新一个 report 元素
        public NormalResult UpdateDailyReportDef(string libraryCode,
    string type,
    ReportDef def)
        {
            // 转换为内部形态
            libraryCode = ToKernel(libraryCode);

            var dom = this.CfgDom;
            var library_node = dom?.DocumentElement?.SelectSingleNode($"library[@code='{libraryCode}']") as XmlElement;
            if (library_node == null)
                return new NormalResult
                {
                    Value = -1,
                    ErrorInfo = $"馆代码 '{libraryCode}' 在配置文件中没有找到"
                };

            var report_nodes = library_node.SelectNodes($"reports/report[@type='{type}']");
            if (report_nodes == null 
                || report_nodes.Count == 0)
                return new NormalResult
                {
                    Value = -1,
                    ErrorInfo = $"馆代码为 '{libraryCode}' 报表类型为 '{type}' 的 report 元素在配置文件中没有找到"
                };

            if (report_nodes != null)
            {
                foreach (XmlElement report in report_nodes)
                {
                    // type 和 cfgFile 暂不允许修改
                    var old_type = report.GetAttribute("type");
                    var old_cfgFile = report.GetAttribute("cfgFile");

                    if (old_type != def.Type)
                        return new NormalResult
                        {
                            Value = -1,
                            ErrorInfo = $"type 不允许修改(当前值为 '{old_type}'，试图修改为 '{def.Type}')"
                        };

                    if (old_cfgFile != def.CfgFile)
                        return new NormalResult
                        {
                            Value = -1,
                            ErrorInfo = $"cfgFile 不允许修改(当前值为 '{old_cfgFile}'，试图修改为 '{def.CfgFile}')"
                        };

                    report.SetAttribute("name", def.Name);
                    report.SetAttribute("frequency", def.Frequency);
                    report.SetAttribute("type", def.Type);
                    report.SetAttribute("cfgFile", def.CfgFile);
                    report.SetAttribute("nameTable", def.NameTable);
                    this.Changed = true;
                    break;
                }
            }

            if (this.Changed == true)
            {
                this.Save();
            }
            return new NormalResult();
        }


        // 删除一个 report 元素
        public NormalResult DeleteDailyReportDef(string libraryCode,
            string type)
        {
            // 转换为内部形态
            libraryCode = ToKernel(libraryCode);

            var dom = this.CfgDom;
            var library_node = dom?.DocumentElement?.SelectSingleNode($"library[@code='{libraryCode}']") as XmlElement;
            if (library_node == null)
                return new NormalResult
                {
                    Value = -1,
                    ErrorInfo = $"馆代码 '{libraryCode}' 在配置文件中没有找到"
                };

            var report_nodes = library_node.SelectNodes($"reports/report[@type='{type}']");
            if (report_nodes != null)
            {
                foreach (XmlElement report in report_nodes)
                {
                    report.ParentNode?.RemoveChild(report);
                    this.Changed = true;
                }
            }

            if (this.Changed == true)
            {
                this.Save();
            }
            return new NormalResult();
        }

        public class NewAllResult : NormalResult
        {
            // 本次实际增加的报表类型
            public List<string>? AddedTypes { get; set; }
        }

        // 增全若干个 report 元素
        public NewAllResult AddDailyReportNewDefAll(string libraryCode)
        {
            // 转换为内部形态
            libraryCode = ToKernel(libraryCode);

            var dom = this.CfgDom;
            var library_node = dom?.DocumentElement?.SelectSingleNode($"library[@code='{libraryCode}']") as XmlElement;
            if (library_node == null)
                return new NewAllResult
                {
                    Value = -1,
                    ErrorInfo = $"馆代码 '{libraryCode}' 在配置文件中没有找到"
                };

            var reports_node = DocumentExtension.CreateElementByPath(library_node, "reports");

            var added_types = new List<string>();
            var all_types = GetAllPredefineReportTypes();
            foreach (var reportType in all_types)
            {
                // 对 report/@type 进行查重
                var exists = reports_node.SelectNodes($"report[@type='{reportType}']");
                if (exists?.Count > 0)
                    continue;

                // 检查 reportType 对应的配置文件是否已经存在
                if (ExistsReportXmlFile(reportType) == false)
                    continue;

                var report_node = dom?.CreateElement("report");
                reports_node.AppendChild(report_node);

                /*
          <report name="部门的借阅排行" 
                frequency="year,month,day" 
                type="101" 
                cfgFile="C:\Users\xietao\dp2Circulation_v2\report_def\101.xml" 
                nameTable="" />
                * */
                // 根据 type 查到 name
                report_node.SetAttribute("name", GetTypeName(reportType));
                report_node.SetAttribute("frequency", "year,month,day");
                report_node.SetAttribute("type", reportType);
                report_node.SetAttribute("cfgFile", GetReportXmlFilePath(reportType));

                this.Changed = true;

                added_types.Add(reportType);
            }

            if (this.Changed == true)
                this.Save();
            // 返回实际增加了多少个 type
            return new NewAllResult
            {
                Value = 0,
                AddedTypes = added_types
            };
        }

        // 获得所有预定义的报表类型
        // 这是通过在报表定义文件子目录中遍历所有 .xml 文件名实现的
        IEnumerable<string> GetAllPredefineReportTypes()
        {
            if (string.IsNullOrEmpty(this.ReportDefDirectory))
                return new List<string>();
            var di = new DirectoryInfo(this.ReportDefDirectory);
            var fis = di.GetFiles("*.xml");
            /*
            List<string> results = new List<string>();
            foreach(var fi in fis)
            {
                results.Add(Path.GetFileNameWithoutExtension(fi.Name));
            }
            results.Sort();
            return results;
            */
            return fis.Select(fi => Path.GetFileNameWithoutExtension(fi.Name)).OrderBy(o => o).ToList(); ;
        }


        bool ExistsReportXmlFile(string type)
        {
            if (string.IsNullOrEmpty(this.ReportDefDirectory))
                return false;
            string fileName = Path.Combine(this.ReportDefDirectory, $"{type}.xml");
            return File.Exists(fileName);
        }

        string GetReportXmlFilePath(string type)
        {
            if (string.IsNullOrEmpty(this.ReportDefDirectory))
                return "";
            return Path.Combine(this.ReportDefDirectory, $"{type}.xml");
        }

        string GetTypeName(string type)
        {
            var fileName = GetReportXmlFilePath(type);
            XmlDocument dom = new XmlDocument();
            try
            {
                dom.Load(fileName);
            }
            catch (FileNotFoundException)
            {
                return "";
            }
            catch (DirectoryNotFoundException)
            {
                return "";
            }

            // root/typeName
            return dom.DocumentElement.GetElementText("typeName");
        }

        #endregion

        public int LoadCfgFile(string strBaseDir,
            string strFileName,
            string strReportDefDirectory,
            out string strError)
        {
            strError = "";

            this.ReportDefDirectory = strReportDefDirectory;

            this.CfgFileName = Path.Combine(strBaseDir, strFileName);  // "report_def.xml"
            this.CfgDom = new XmlDocument();
            try
            {
                this.CfgDom.Load(this.CfgFileName);

                // 2017/5/17
                if (this.CfgDom.DocumentElement == null)
                {
                    strError = "文件 '" + this.CfgFileName + "' 缺乏根元素。\r\n内容为 [" + this.CfgDom.OuterXml + "]";
                    return -1;
                }
            }
            catch (FileNotFoundException)
            {
                this.CfgDom.LoadXml("<root />");
            }
            catch (Exception ex)
            {
                this.CfgDom = null;
                strError = "报表配置文件 " + this.CfgFileName + " 打开错误: " + ex.Message;
                return -1;
            }

            return 0;
        }

        // 2017/5/17
        // 在 XML 文件装载时发现格式不正确，可用本函数初始化 CfgDom，达到清空内容，重新配置的准备状态
        public void InitializeCfgDom()
        {
            this.CfgDom = new XmlDocument();
            this.CfgDom.LoadXml("<root />");
        }

        public void Save()
        {
            if (this.CfgDom != null && string.IsNullOrEmpty(this.CfgFileName) == false)
            {
                this.CfgDom.Save(this.CfgFileName);
            }

            this.Changed = false;
        }

#if REMOVED
        public void FillList(ListView list)
        {
            list.Items.Clear();

            if (this.CfgDom == null || this.CfgDom.DocumentElement == null)
                return;

            XmlNodeList nodes = this.CfgDom.DocumentElement.SelectNodes("library");
            foreach (XmlNode node in nodes)
            {
                string strCode = DomUtil.GetAttr(node, "code");

                strCode = ReportForm.GetDisplayLibraryCode(strCode);

                ListViewItem item = new ListViewItem();
                ListViewUtil.ChangeItemText(item, 0, strCode);

                list.Items.Add(item);
            }

            if (list.Items.Count > 0)
                list.Items[0].Selected = true;
        }

#endif

        // 获得 102 表的部门列表
        public List<string> Get_102_Departments(string strLibraryCode)
        {
            if (this.CfgDom == null || this.CfgDom.DocumentElement == null)
                return new List<string>();

            var node = this.CfgDom?.DocumentElement?.SelectSingleNode("library[@code='" + strLibraryCode + "']") as XmlElement;
            if (node == null)
                return new List<string>();

            return StringUtil.SplitList(node.GetAttribute("table_102_departments"));
        }

        public XmlElement? GetLibraryNode(string strLibraryCode)
        {
            if (this.CfgDom == null || this.CfgDom.DocumentElement == null)
                return null;

            return this.CfgDom?.DocumentElement?.SelectSingleNode("library[@code='" + strLibraryCode + "']") as XmlElement;
        }

        // 创建一个新的 <library> 元素。要对 code 属性进行查重
        // parameters:
        //      -1  出错
        //      0   成功
        //      1   已经有这个 code 属性的元素了
        public int CreateNewLibraryNode(string strLibraryCode,
            out XmlElement? node,
            out string strError)
        {
            strError = "";
            node = null;

            if (this.CfgDom == null || this.CfgDom.DocumentElement == null)
            {
                strError = "ReportConfig 对象尚未初始化";
                return -1;
            }

            node = this.CfgDom?.DocumentElement?.SelectSingleNode("library[@code='" + strLibraryCode + "']") as XmlElement;
            if (node != null)
            {
                strError = "已经存在馆代码 '" + strLibraryCode + "' 的配置事项了，不能重复创建";
                return 1;
            }

            node = this.CfgDom?.CreateElement("library");
            this.CfgDom?.DocumentElement.AppendChild(node);
            node?.SetAttribute("code", strLibraryCode);

            this.Changed = true;
            return 0;
        }

        // parameters:
        //      nodeLibrary 配置文件中的 library 元素节点。如果为 null，表示取全局缺省的模板
        public static int LoadHtmlTemplate(XmlElement nodeLibrary,
            string strCssTemplateDir,
            out string strTemplate,
            out string strError)
        {
            strTemplate = "";
            strError = "";

            // string strCssTemplateDir = Path.Combine(Program.MainForm.UserDir, "report_def");   //  Path.Combine(Program.MainForm.UserDir, "report_def");

            string strHtmlTemplate = "";

            if (nodeLibrary != null)
                strHtmlTemplate = nodeLibrary.GetAttribute("htmlTemplate");

            if (string.IsNullOrEmpty(strHtmlTemplate) == true)
                strHtmlTemplate = "default";

            string strFileName = Path.Combine(strCssTemplateDir, DailyReporting.GetValidPathString(strHtmlTemplate) + ".css");
            if (File.Exists(strFileName) == false)
            {
                strError = "CSS 模板文件 '" + strFileName + "' 不存在";
                return -1;
            }

            Encoding encoding;
            // return:
            //      -1  出错 strError中有返回值
            //      0   文件不存在 strError中有返回值
            //      1   文件存在
            //      2   读入的内容不是全部
            int nRet = FileUtil.ReadTextFileContent(strFileName,
                -1,
                out strTemplate,
                out encoding,
                out strError);
            if (nRet != 1)
                return -1;

            return 0;
        }
    }

    /*
  <report 
name="部门的借阅排行"
frequency="year,month,day" 
type="101" 
cfgFile="C:\Users\xietao\dp2Circulation_v2\report_def\101.xml" 
nameTable="" />
 * */
    // 一个报表定义结构
    public class ReportDef
    {
        public string? Name { get; set; }

        public string? Frequency { get; set; }

        public string? Type { get; set; }

        public string? CfgFile { get; set; }

        public string? NameTable { get; set; }
    }
}
