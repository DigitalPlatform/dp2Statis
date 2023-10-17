using System.ComponentModel.DataAnnotations;

namespace dp2StatisServer.ViewModels
{
    // 单个报表
    public class ReportViewModel
    {
        [Display(Name = "实例名")]
        public string? InstanceName { get; set; }

        [Display(Name = "描述")]
        public string? Description { get; set; }

        [Display(Name = "报表类型")]
        public string? ReportType { get; set; }

        [Display(Name = "馆代码")]
        public string? LibraryCode { get; set; }

        [Display(Name = "时间范围")]
        public string? DateRange { get; set; }

        [Display(Name = "排序列")]
        public string? SortColumn { get; set; }


        [Display(Name = "其它参数")]
        public string? OtherParameters { get; set; }


        public string? XmlContent { get; set; }

        public string? HtmlContent { get; set; }

        public string? ErrorInfo { get; set; }

        public string? SuccessInfo { get; set; }
    }
}
