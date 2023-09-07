using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

using dp2StatisServer.Data;

namespace dp2StatisServer.ViewModels
{
    public class ManageViewModel
    {
        [Display(Name = "数据根目录")]
        public string? DataDirRoot { get; set; }

        [Display(Name = "实例列表")]
        public IEnumerable<Instance> Instances { get; set; }

        public string? ErrorInfo { get; set; }

        public string? SuccessInfo { get; set; }

        public ManageViewModel()
        {
            this.Instances = new List<Instance>();
        }
    }

}
