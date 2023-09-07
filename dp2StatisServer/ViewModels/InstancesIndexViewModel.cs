using dp2StatisServer.Data;
using System.ComponentModel.DataAnnotations;

namespace dp2StatisServer.ViewModels
{
    // 用于从全部实例中选择一个实例
    public class InstancesIndexViewModel
    {
        [Display(Name = "实例列表")]
        public IEnumerable<Instance> Instances { get; set; }

        public string? SelectedInstanceName { get; set; }

        public string? ErrorInfo { get; set; }

        public string? SuccessInfo { get; set; }

        public InstancesIndexViewModel()
        {
            this.Instances = new List<Instance>();
        }
    }
}
