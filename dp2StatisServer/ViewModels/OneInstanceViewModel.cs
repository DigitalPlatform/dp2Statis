using System.ComponentModel.DataAnnotations;

namespace dp2StatisServer.ViewModels
{
    // 用于统计功能的单个 Instance
    public class OneInstanceViewModel
    {
        [Display(Name = "实例名")]
        public string? InstanceName { get; set; }

        [Display(Name = "描述")]
        public string? Description { get; set; }

        [Display(Name = "同步时间")]
        public string? ReplicateTime { get; set; }

        public string? ErrorInfo { get; set; }

        public string? SuccessInfo { get; set; }
    }
}
