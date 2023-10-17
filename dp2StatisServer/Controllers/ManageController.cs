using Microsoft.AspNetCore.Mvc;
using dp2StatisServer.Data;
using dp2StatisServer.ViewModels;

namespace dp2StatisServer.Controllers
{
    public class ManageController : BaseController
    {
        public ManageController(ILogger<HomeController> logger,
    IInstanceRepository instanceRepository) : base(logger, instanceRepository) 
        {
        }

        public IActionResult Index()
        {
            if (this.IsSupervisor() == false)
                return RedirectToAction("Login", "Home");

            return View(NewManageModel());
        }

        ManageViewModel NewManageModel(ManageViewModel? input_model = null)
        {
            var model = input_model;
            if (model == null)
                model = new ManageViewModel();

            var repository = GetInstanceRepository();

            model.DataDirRoot = repository.GetGlobalInfo()?.DataDirRoot;
            model.Instances = GetInstanceRepository().GetInstances();
            return model;
        }

        #region CreateInstance

        [HttpGet]
        public IActionResult CreateInstance()
        {
            if (this.IsSupervisor() == false)
                return RedirectToAction("Home", "Home");

            var model = new EditInstanceViewModel
            {
                Action = "create"
            };
            return View("EditInstance", model);
        }

        [HttpPost]
        public IActionResult CreateInstance(EditInstanceViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var instanceRepository = GetInstanceRepository();
                    var instance = instanceRepository.CreateInstance(model.InstanceName,
                        model.Description,
                        model.ReplicateTime,
                        model.AppServerUrl,
                        model.AppServerUserName,
                        model.AppServerPassword,
                        model.AdminPassword,
                        model.InstanceUserPassword);
                    model.SuccessInfo = $"成功创建实例 {model.InstanceName}";
                }
                catch (Exception ex)
                {
                    model.ErrorInfo = ex.Message;
                }
            }

            return View("EditInstance", model);
        }

        #endregion

        #region ChangeInstance

        [HttpGet]
        public IActionResult ChangeInstance(string name)
        {
            if (this.IsSupervisor() == false)
                return RedirectToAction("Home", "Home");

            var instance = GetInstanceRepository().FindInstance(name);
            if (instance == null)
            {
                var model = new EditInstanceViewModel
                {
                    Action = "change",
                    ErrorInfo = $"名为 '{name}' 的实例不存在"
                };
                return View("EditInstance", model);
            }

            {
                var model = new EditInstanceViewModel(instance)
                {
                    Action = "change",
                    AdminPassword = "{dontchange}"
                };
                return View("EditInstance", model);
            }
        }

        [HttpPost]
        public IActionResult ChangeInstance(EditInstanceViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var name = model.InstanceName;
                    if (name == null)
                    {
                        model.ErrorInfo = "实例名不允许为空";
                        return View("EditInstance", model);
                    }
                    var instance = GetInstanceRepository().FindInstance(name);
                    if (instance == null)
                    {
                        model.ErrorInfo = $"名为 '{name}' 的实例不存在";
                        return View("EditInstance", model);
                    }

                    {
                        // instance.Name 不允许修改
                        // Pgsql 几个参数也不允许修改

                        instance.Description = model.Description;
                        instance.ReplicateTime = model.ReplicateTime;

                        instance.AppServerUrl = model.AppServerUrl;
                        instance.AppServerUserName = model.AppServerUserName;
                        instance.AppServerPassword = model.AppServerPassword;
                    }

                    GetInstanceRepository().SetInstance(instance);
                    model.SuccessInfo = $"成功修改实例 {model.InstanceName}";
                }
                catch (Exception ex)
                {
                    model.ErrorInfo = ex.Message;
                }
            }

            return View("EditInstance", model);
        }

        #endregion

        #region DeleteInstance

        [HttpGet]
        public IActionResult DeleteInstance(string name)
        {
            if (this.IsSupervisor() == false)
                return RedirectToAction("Home", "Home");

            var instance = GetInstanceRepository().FindInstance(name);
            if (instance == null)
            {
                var model = new EditInstanceViewModel
                {
                    Action = "delete",
                    ErrorInfo = $"名为 '{name}' 的实例不存在"
                };
                return View("EditInstance", model);
            }

            {
                var model = new EditInstanceViewModel(instance)
                {
                    Action = "delete",
                    AdminPassword = null
                };
                return View("EditInstance", model);
            }
        }

        [HttpPost]
        public IActionResult DeleteInstance(EditInstanceViewModel model)
        {
            if (this.IsSupervisor() == false)
                return RedirectToAction("Home", "Home");

            if (ModelState.IsValid)
            {
                try
                {
                    var name = model.InstanceName;
                    if (name == null)
                    {
                        model.ErrorInfo = "实例名不允许为空";
                        return View("EditInstance", model);
                    }

                    var adminPassword = model.AdminPassword;
                    if (adminPassword == "{dontChange}"
                        || adminPassword == null)
                    {
                        model.ErrorInfo = "请输入 postgres 账户密码";
                        return View("EditInstance", model);
                    }

                    GetInstanceRepository().DeleteInstance(name, model.AdminPassword);

                    model.SuccessInfo = $"成功删除实例 {name}";
                }
                catch (Exception ex)
                {
                    model.ErrorInfo = ex.Message;
                }
            }

            return View("EditInstance", model);
        }

        #endregion
    }
}
