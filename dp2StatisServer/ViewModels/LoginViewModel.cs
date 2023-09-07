using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace dp2StatisServer.ViewModels
{
    public class LoginViewModel
    {
        // 如果为 null，表示要登录到 dp2003.com/dp2library
        [Display(Name = "实例名")]
        public string? InstanceName { get; set; }

        [Display(Name = "用户名")]
        [Required(ErrorMessage = "请输入用户名")]
        public string? UserName { get; set; }

        [Display(Name = "密码")]
        [Required(ErrorMessage = "请输入密码")]
        public string? Password { get; set; }

        [Display(Name = "错误信息")]
        public string? ErrorInfo { get; set; }
    }
}
