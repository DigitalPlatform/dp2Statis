using System.ComponentModel.DataAnnotations;

using dp2Statis.Reporting;

namespace dp2StatisServer.ViewModels
{
    // 用于编辑一个报表定义的 ViewModel 类
    public class DailyReportOneReportViewModel
    {
        [Display(Name = "馆代码")]
        public string? LibraryCode { get; set; }

        [Display(Name = "报表名")]
        public string? Name { get; set; }

        [Display(Name = "创建频率")]
        public string? Frequency { get; set; }

        [Display(Name = "报表类型")]
        public string? Type { get; set; }

        [Display(Name = "配置文件路径")]
        public string? CfgFile { get; set; }

        [Display(Name = "名字列表")]
        public string? NameTable { get; set; }


        public string? ErrorInfo { get; set; }

        public string? SuccessInfo { get; set; }

        public static DailyReportOneReportViewModel FromReportDef(ReportDef def)
        {
            return new DailyReportOneReportViewModel
            {
                Name = def.Name,
                Frequency = def.Frequency,
                Type = def.Type,
                CfgFile = def.CfgFile,
                NameTable = def.NameTable,
            };
        }

        public ReportDef ToReportDef()
        {
            return new ReportDef
            {
                Name = Name,
                Frequency = Frequency,
                Type = Type,
                CfgFile = CfgFile,
                NameTable = NameTable,
            };
        }
    }
}
