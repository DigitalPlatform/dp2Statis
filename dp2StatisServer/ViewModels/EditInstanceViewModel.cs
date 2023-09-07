using dp2StatisServer.Data;
using System.ComponentModel.DataAnnotations;


namespace dp2StatisServer.ViewModels
{
    // 用于增删改的 Instance
    public class EditInstanceViewModel
    {
        // 动作
        // create change delete 之一
        public string? Action { get; set; }

        [Display(Name = "实例名")]
        public string? InstanceName { get; set; }


        [Display(Name = "描述")]
        public string? Description { get; set; }

        [Display(Name = "同步时间")]
        public string? ReplicateTime { get; set; }

        #region dp2library 服务器

        [Display(Name = "dp2library 服务器 URL")]
        public string? AppServerUrl { get; set; }

        [Display(Name = "dp2library 用户名")]
        public string? AppServerUserName { get; set; }

        // 如何保持密码在页面提交后不被清除
        // https://stackoverflow.com/questions/21794016/retain-the-password-text-box-values-after-submit-in-mvc
        [Display(Name = "dp2library 密码")]
        public string? AppServerPassword { get; set; }

        #endregion

        #region Pgsql setup

        // 以下两个密码用于 setup pgsql user
        [Display(Name = "postgres 账户密码")]
        public string? AdminPassword { get; set; }

        [Display(Name = "实例账户密码")]
        public string? InstanceUserPassword { get; set; }

        /*
        // 是否显示 setup pgsql user 部分面板
        public bool UseSetupPgsql { get; set; }
        */

        #endregion

        public string? ErrorInfo { get; set; }

        public string? SuccessInfo { get; set; }


        public EditInstanceViewModel()
        {

        }

        public EditInstanceViewModel(Instance instance)
        {
            this.InstanceName = instance.Name;
            this.Description = instance.Description;
            this.ReplicateTime = instance.ReplicateTime;
            this.AppServerUrl = instance.AppServerUrl;
            this.AppServerUserName = instance.AppServerUserName;
            this.AppServerPassword = instance.AppServerPassword;
            this.AdminPassword = null;
            this.InstanceUserPassword = instance.DbConfig?.Password;
        }
    }

}
