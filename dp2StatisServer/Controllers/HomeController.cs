using Microsoft.AspNetCore.Mvc;
using dp2StatisServer.Data;
using dp2StatisServer.ViewModels;

namespace dp2StatisServer.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(ILogger<HomeController> logger,
    IInstanceRepository itemRepository) : base(logger, itemRepository)
        {
        }

        public IActionResult Index()
        {
            return View();
        }

        #region login

        [HttpGet]
        // https://www.c-sharpcorner.com/article/simple-login-application-using-Asp-Net-mvc/
        public ActionResult Login(string? instance_name)
        {
            return View(new LoginViewModel
            {
                InstanceName = instance_name
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                string url = "http://dp2003.com/dp2library/rest";
                if (string.IsNullOrEmpty(model.InstanceName) == false)
                {
                    var instance = ServerContext.Instances.FindInstance(model.InstanceName);
                    if (instance == null)
                    {
                        model.ErrorInfo = $"实例 '{model.InstanceName}' 没有找到";
                        return View(model);
                    }
                    if (string.IsNullOrEmpty(instance.AppServerUrl))
                    {
                        model.ErrorInfo = $"实例 '{model.InstanceName}' 尚未配置 AppServerUrl 参数，无法用于登录";
                        return View(model);
                    }
                    url = instance.AppServerUrl;
                }

                if (string.IsNullOrEmpty(model.UserName))
                {
                    model.ErrorInfo = $"用户名不应为空";
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.Password))
                {
                    model.ErrorInfo = $"密码不应为空";
                    return View(model);
                }

                RestChannel channel = new RestChannel(url);
                var result = channel.Login(model.UserName,
                    model.Password,
                    $"location=dp2Statis,type=worker,client=dp2Statis|0.01"
                    );
                string? error = result?.LoginResult?.ErrorInfo;
                if (result?.LoginResult?.Value == 1)
                {
                    bool has_right;
                    if (string.IsNullOrEmpty(model.InstanceName) == false)
                    {
                        has_right = Include(result?.strRights, "_statis_instance");
                    }
                    else
                    {
                        has_right = Include(result?.strRights, "_statis_supervisor");
                    }

                    if (has_right)
                    {
                        this.SetUserName(model.UserName,
                            model.InstanceName == null ? "" : model.InstanceName);
                        return RedirectToAction("UserDashBoard", new { instance_name = model.InstanceName });
                    }
                    else
                        error = "权限不足";
                }

                model.ErrorInfo = error;
                this.RemoveSessionItem();
            }
            return View(model);
        }

        public ActionResult UserDashBoard(string? instance_name)
        {
            if (this.IsSupervisor() || this.HttpContext.IsInstanceManager(instance_name))
            {
                return View();
            }
            else
            {
                return RedirectToAction("Login");
            }
        }

        public ActionResult Logout()
        {
            var instance_name = (string?)this.Request.RouteValues["name"];
            this.RemoveSessionItem();
            return RedirectToAction("Login", new { instance_name = instance_name});
        }



        #endregion

        static bool Include(string? text, string right)
        {
            if (text == null)
                return false;
            var rights = text.Split(',');
            return rights.Contains(right);
        }
    }
}
