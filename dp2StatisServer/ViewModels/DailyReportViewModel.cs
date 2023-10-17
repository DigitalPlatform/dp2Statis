using dp2Statis.Reporting;
using dp2StatisServer.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace dp2StatisServer.ViewModels
{
    // 每日报表
    // 针对一个实例的创建每日报表的定义和操作的 ViewModel 类
    public class DailyReportViewModel
    {
        [Display(Name = "实例名")]
        public string? InstanceName { get; set; }

        [Display(Name = "描述")]
        public string? Description { get; set; }


        // *** 可用的馆代码列表
        public ICollection<string>? LibraryCodeList { get; set; }

        [Display(Name = "正在查看报表定义的馆代码")]
        public string? CurrentLibraryCode { get; set; }


        [Display(Name = "拟新增的馆代码")]
        public string? NewLibraryCode { get; set; }

        // *** CurrentLibraryCode 馆的所有报表定义
        public ICollection<ReportDef>? ReportDefs { get; set; }

        [Display(Name = "拟新增的报表类型")]
        public string? NewReportType { get; set; }



        public string? ErrorInfo { get; set; }

        public string? SuccessInfo { get; set; }


        public void FillData(Instance instance)
        {
            this.InstanceName = instance.Name;
            this.Description = instance.Description;

            var list = instance.GetDailyReportDefDom().GetDailyReportDefLibraryCodeList("display");
            this.LibraryCodeList = list;
            if (this.CurrentLibraryCode == null
                && this.LibraryCodeList?.Count > 0)
            {
                this.CurrentLibraryCode = list.FirstOrDefault();
            }

            if (this.CurrentLibraryCode != null)
                this.ReportDefs = instance.GetDailyReportDefDom().GetReportDefs(this.CurrentLibraryCode);
            else
                this.ReportDefs = null;
        }

        public static List<SelectListItem> GetLibraryCodeList(
            string? instance_name,
            string? selected = null)
        {
            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
                return new List<SelectListItem>();
            var names = instance.GetLibraryCodes();
            names.Insert(0, "[全局]");
            var items = new List<SelectListItem>();
            foreach (var name in names)
            {
                items.Add(new SelectListItem
                {
                    Text = name,
                    Value = name,
                    Selected = name == selected
                });
            }
            return items;
        }
    }

}
