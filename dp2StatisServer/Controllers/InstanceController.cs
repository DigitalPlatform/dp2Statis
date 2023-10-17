using System;
using System.IO;
using System.Xml;
using System.Collections;

using Microsoft.AspNetCore.Mvc;

using dp2StatisServer.Data;
using dp2StatisServer.ViewModels;

using DigitalPlatform.LibraryServer.Reporting;
using DigitalPlatform.LibraryClientOpenApi;
using DigitalPlatform;
using dp2Statis.Reporting;
using Newtonsoft.Json;
using DocumentFormat.OpenXml.Wordprocessing;
using DigitalPlatform.Text;

namespace dp2StatisServer.Controllers
{
    [Route("{controller=Instance}/{name=}/{action=Index}")]
    public class InstanceController : Controller
    {
        public IActionResult Index()
        {
            var instance_name = (string?)this.Request.RouteValues["name"];
            if (string.IsNullOrEmpty(instance_name)
                || instance_name == "[default]")
                return View(NewIndexModel());

            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            // return View(NewInstanceModel(instance_name));
            return View(NewIndexModel());
        }

        InstancesIndexViewModel NewIndexModel(string? name = null)
        {
            var model = new InstancesIndexViewModel();
            model.Instances = ServerContext.Instances.GetInstances();
            return model;
        }

        OneInstanceViewModel NewInstanceModel(string instance_name)
        {
            var model = new OneInstanceViewModel();

            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
            {
                model.ErrorInfo = $"实例名 '{instance_name}' 没有找到";
                return model;
            }
            model.InstanceName = instance.Name;
            model.Description = instance.Description;
            model.ReplicateTime = instance.ReplicateTime;
            return model;
        }

        // 呈现“复制”页面
        [HttpGet]
        public IActionResult Replication()
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            var model = new OneInstanceViewModel();

            var instance_name = (string?)this.Request.RouteValues["name"];
            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
            {
                model.ErrorInfo = $"实例名 '{instance_name}' 没有找到";
                return View(model);
            }

            model.InstanceName = instance.Name;
            model.Description = instance.Description;
            model.ReplicateTime = instance.ReplicateTime;
            return View(model);
        }



        [HttpPost]
        public IActionResult RecreateDatabase(OneInstanceViewModel model)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            if (ModelState.IsValid)
            {
                try
                {
                    var name = model.InstanceName;
                    if (name == null)
                    {
                        model.ErrorInfo = "实例名不允许为空";
                        return View("Replication", model);
                    }

                    var instance = ServerContext.Instances.FindInstance(name);
                    if (instance == null)
                    {
                        model.ErrorInfo = $"实例名 '{name}' 没有找到";
                        return View("Replication", model);
                    }

                    UpdateInstance(model, instance);

                    RecreateDatabase(instance);
                    model.SuccessInfo = $"为实例 {name} 重新创建数据库成功";
                }
                catch (Exception ex)
                {
                    model.ErrorInfo = ex.Message;
                }
            }

            return View("Replication", model);
        }

        // 修改同步时间
        [HttpPost]
        public IActionResult ChangeReplicateTime(OneInstanceViewModel model)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            if (ModelState.IsValid)
            {
                try
                {
                    var name = model.InstanceName;
                    if (name == null)
                    {
                        model.ErrorInfo = "实例名不允许为空";
                        return View("Replication", model);
                    }

                    var instance = ServerContext.Instances.FindInstance(name);
                    if (instance == null)
                    {
                        model.ErrorInfo = $"实例名 '{name}' 没有找到";
                        return View("Replication", model);
                    }

                    UpdateInstance(model, instance);

                    model.SuccessInfo = $"实例 {name} 的同步时间已经被修改为 {instance.ReplicateTime}";
                }
                catch (Exception ex)
                {
                    model.ErrorInfo = ex.Message;
                }
            }

            return View("Replication", model);
        }

        [HttpPost]
        public IActionResult StopReplication(OneInstanceViewModel model)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            if (ModelState.IsValid)
            {
                try
                {
                    var name = model.InstanceName;
                    if (name == null)
                    {
                        model.ErrorInfo = "实例名不允许为空";
                        return View("Replication", model);
                    }

                    var instance = ServerContext.Instances.FindInstance(name);
                    if (instance == null)
                    {
                        model.ErrorInfo = $"实例名 '{name}' 没有找到";
                        return View("Replication", model);
                    }

                    UpdateInstance(model, instance);

                    // TODO: 放入一个单独的 task 运行。输出文本 SSE 推送给浏览器
                    instance.CancelReplication();
                    /*
                    if (result.Value == -1)
                    {
                        model.ErrorInfo = result.ErrorInfo;
                        return View("Replication", model);
                    }
                    */
                    model.SuccessInfo = $"为实例 {name} 停止复制成功";
                }
                catch (Exception ex)
                {
                    model.ErrorInfo = ex.Message;
                }
            }

            return View("Replication", model);
        }

        static void UpdateInstance(OneInstanceViewModel model,
            Instance instance)
        {
            if (model.ReplicateTime != instance.ReplicateTime)
            {
                instance.ReplicateTime = model.ReplicateTime;
                instance._setData();    // 可能抛出异常
                instance.SaveConfigFile();
                // 重新注册定时事件
                ServerContext.RefreshJob(instance);
            }
        }

        RedirectToActionResult RedirectToLogin()
        {
            var instance_name = (string?)this.Request.RouteValues["name"];
            return RedirectToAction("Login", "Home", new { instance_name = instance_name });
        }

        [HttpPost]
        public IActionResult StartReplication(OneInstanceViewModel model)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            if (ModelState.IsValid)
            {
                try
                {
                    var name = model.InstanceName;
                    if (name == null)
                    {
                        model.ErrorInfo = "实例名不允许为空";
                        return View("Replication", model);
                    }

                    var instance = ServerContext.Instances.FindInstance(name);
                    if (instance == null)
                    {
                        model.ErrorInfo = $"实例名 '{name}' 没有找到";
                        return View("Replication", model);
                    }

                    UpdateInstance(model, instance);

                    // TODO: 放入一个单独的 task 运行。输出文本 SSE 推送给浏览器
                    _ = instance.BeginReplication(!instance.HasTaskDom);
                    /*
                    if (result.Value == -1)
                    {
                        model.ErrorInfo = result.ErrorInfo;
                        return View("Replication", model);
                    }
                    */
                    model.SuccessInfo = $"为实例 {name} 启动复制成功";
                }
                catch (Exception ex)
                {
                    model.ErrorInfo = ex.Message;
                }
            }

            return View("Replication", model);
        }


        // https://www.c-sharpcorner.com/blogs/server-side-events-in-asp-net-mvc
        // https://makolyte.com/aspnet-async-sse-endpoint/
        [HttpGet]
        public async Task Message(string name)
        {
            // TODO: IsInstanceManagerOrSupervisor() 判断

            var instance = ServerContext.Instances.FindInstance(name);
            if (instance == null)
                return;

            Response.ContentType = "text/event-stream";
            // await SendMessageAsync(instance);
            await instance.SendMessageAsync(async (s) =>
            {
                await Response.WriteAsync(s);
            },
            HttpContext.RequestAborted);

#if REMOVED
            Response.ContentType = "text/event-stream";

            DateTime startDate = DateTime.Now;
            while (startDate.AddMinutes(1) > DateTime.Now)
            {
                if (HttpContext.RequestAborted.IsCancellationRequested)
                    break;
                await Response.WriteAsync(string.Format("data: {0}\n\n", DateTime.Now.ToString()));
                // Response.Flush();

                System.Threading.Thread.Sleep(1000);
            }

            // Response.Close();
#endif

#if REMOVED
            //1. Set content type
            Response.ContentType = "text/event-stream";
            Response.StatusCode = 200;

            //Disable buffering on this response so it sends it out before it is done
            var responseFeature = HttpContext.Features.Get<IHttpResponseBodyFeature>();
            responseFeature?.DisableBuffering();

            using (StreamWriter streamWriter = new StreamWriter(Response.Body))
            {
                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                    //2. Await something that generates messages
                    await Task.Delay(1000, HttpContext.RequestAborted);

                    //3. Write to the Response.Body stream
                    await streamWriter.WriteAsync($"{DateTime.Now} Looping\n\n");
                    await streamWriter.FlushAsync();

                }
            }
#endif
        }

        #region 每日报表

        [Serializable]
        public class JsonResponseViewModel
        {
            public int ResponseCode { get; set; }

            public string ResponseMessage { get; set; } = string.Empty;
        }

        [HttpGet]
        public JsonResult GetNameTable(string libraryCode)
        {
            var instance_name = (string?)this.Request.RouteValues["name"];
            var instance = ServerContext.Instances.FindInstance(instance_name);

            libraryCode = ReportConfigBuilder.ToKernel(libraryCode);
            using (var context = new LibraryContext(instance.DbConfig))
            {
                var names = Report.GetAllReaderDepartments(context, libraryCode);
                var model = new JsonResponseViewModel();
                if (names != null)
                {
                    model.ResponseCode = 0;
                    model.ResponseMessage = string.Join(',', names); // JsonConvert.SerializeObject(student);
                }
                else
                {
                    model.ResponseCode = 1;
                    model.ResponseMessage = "No name available";
                }
                return Json(model);
            }
        }

        [HttpPost]
        public IActionResult StopDailyReporting(DailyReportViewModel model)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            if (ModelState.IsValid)
            {
                try
                {
                    var name = model.InstanceName;
                    if (name == null)
                    {
                        model.ErrorInfo = "实例名不允许为空";
                        return View("DailyReport", model);
                    }

                    var instance = ServerContext.Instances.FindInstance(name);
                    if (instance == null)
                    {
                        model.ErrorInfo = $"实例名 '{name}' 没有找到";
                        return View("DailyReport", model);
                    }

                    model.FillData(instance);

                    // TODO: 放入一个单独的 task 运行。输出文本 SSE 推送给浏览器
                    instance.CancelDailyReporting();
                    /*
                    if (result.Value == -1)
                    {
                        model.ErrorInfo = result.ErrorInfo;
                        return View("Replication", model);
                    }
                    */
                    model.SuccessInfo = $"为实例 {name} 停止每日报表成功";
                }
                catch (Exception ex)
                {
                    model.ErrorInfo = ex.Message;
                }
            }

            return View("DailyReport", model);
        }

        [HttpPost]
        public IActionResult StartDailyReporting(DailyReportViewModel model)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            if (ModelState.IsValid)
            {
                try
                {
                    var name = model.InstanceName;
                    if (name == null)
                    {
                        model.ErrorInfo = "实例名不允许为空";
                        return View("DailyReport", model);
                    }

                    var instance = ServerContext.Instances.FindInstance(name);
                    if (instance == null)
                    {
                        model.ErrorInfo = $"实例名 '{name}' 没有找到";
                        return View("DailyReport", model);
                    }

                    model.FillData(instance);

                    // TODO: 放入一个单独的 task 运行。输出文本 SSE 推送给浏览器
                    _ = instance.BeginDailyReporting();
                    /*
                    if (result.Value == -1)
                    {
                        model.ErrorInfo = result.ErrorInfo;
                        return View("Replication", model);
                    }
                    */
                    model.SuccessInfo = $"为实例 {name} 启动每日报表成功";
                }
                catch (Exception ex)
                {
                    model.ErrorInfo = ex.Message;
                }
            }

            return View("DailyReport", model);
        }


        [HttpGet]
        public IActionResult DailyReport(string? libraryCode = null)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            var model = new DailyReportViewModel();

            var instance_name = (string?)this.Request.RouteValues["name"];
            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
            {
                model.ErrorInfo = $"实例名 '{instance_name}' 没有找到";
                return View("DailyReport", model);
            }

            model.CurrentLibraryCode = libraryCode;
            model.FillData(instance);
            return View("DailyReport", model);
        }

        // 为每日报表配置文件添加一个 library 元素
        [HttpPost]
        public IActionResult AddDailyReportLibraryCode(DailyReportViewModel model)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            var instance_name = (string?)this.Request.RouteValues["name"];
            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
            {
                model.ErrorInfo = $"实例名 '{instance_name}' 没有找到";
                return View("DailyReport", model);
            }
            if (model.NewLibraryCode == null)
            {
                model.ErrorInfo = $"请输入拟创建的馆代码";
                model.FillData(instance);
                return View("DailyReport", model);
            }

            // dp2library 服务器中的全部可用馆代码
            var list = instance.GetLibraryCodes();
            string newLibraryCode = ReportConfigBuilder.ToKernel(model.NewLibraryCode);
            if (list.Contains(newLibraryCode) == false)
            {
                model.ErrorInfo = $"馆代码 '{model.NewLibraryCode}' 不是已定义的合法馆代码";
                model.FillData(instance);
                return View("DailyReport", model);
            }

            var ret = instance.GetDailyReportDefDom().AddDailyReportLibraryCode(newLibraryCode);
            if (ret.Value == -1)
            {
                model.ErrorInfo = ret.ErrorInfo;
                model.FillData(instance);
                return View("DailyReport", model);
            }

            string library_code = model.NewLibraryCode;

            model.FillData(instance);
            model.NewLibraryCode = null;
            model.SuccessInfo = $"成功添加馆代码为 '{library_code}' 的报表定义";
            return View("DailyReport", model);
        }

        // 为每日报表配置文件增全若干个 library 元素
        [HttpPost]
        public IActionResult AddDailyReportLibraryCodeAll(DailyReportViewModel model)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            var instance_name = (string?)this.Request.RouteValues["name"];
            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
            {
                model.ErrorInfo = $"实例名 '{instance_name}' 没有找到";
                return View("DailyReport", model);
            }

            // 已经存在报表定义的馆代码
            var exists_list = instance.GetDailyReportDefDom().GetDailyReportDefLibraryCodeList("");
            // dp2library 服务器中的全部可用馆代码
            var list = instance.GetLibraryCodes();
            var added_codes = new List<string>();
            foreach (var code in list)
            {
                if (exists_list.Contains(code))
                    continue;
                var ret = instance.GetDailyReportDefDom().AddDailyReportLibraryCode(code);
                if (ret.Value == -1)
                {
                    model.ErrorInfo = ret.ErrorInfo;
                    model.FillData(instance);
                    return View("DailyReport", model);
                }
                added_codes.Add(code);
            }

            model.FillData(instance);
            model.NewLibraryCode = null;
            if (added_codes.Count == 0)
                model.SuccessInfo = $"没有添加任何馆代码的报表定义";
            else
                model.SuccessInfo = $"成功添加馆代码为 {string.Join(',', added_codes)} 的报表定义";  // TODO: 把 "" 替换为 "[全局]"
            return View("DailyReport", model);
        }


        // 为每日报表的配置文件删除一个 library 元素
        [HttpPost]
        public IActionResult DeleteDailyReportLibraryCode
            (DailyReportViewModel model,
            string? libraryCode)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            var instance_name = (string?)this.Request.RouteValues["name"];
            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
            {
                model.ErrorInfo = $"实例名 '{instance_name}' 没有找到";
                return View("DailyReport", model);
            }
            if (libraryCode == null)
            {
                model.ErrorInfo = $"请指定拟删除的馆代码";
                model.FillData(instance);
                return View("DailyReport", model);
            }
            var ret = instance.GetDailyReportDefDom().DeleteDailyReportLibraryCode(libraryCode);
            if (ret.Value == -1)
            {
                model.ErrorInfo = ret.ErrorInfo;
                model.FillData(instance);
                return View("DailyReport", model);
            }
            if (model.CurrentLibraryCode == libraryCode)
                model.CurrentLibraryCode = null;
            model.FillData(instance);
            model.SuccessInfo = $"成功删除馆代码为 '{libraryCode}' 的报表定义";
            return View("DailyReport", model);
        }

        // 为每日报表的配置文件添加一个 report 元素
        [HttpPost]
        public IActionResult AddDailyReportNewDef(DailyReportViewModel model)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            var instance_name = (string?)this.Request.RouteValues["name"];
            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
            {
                model.ErrorInfo = $"实例名 '{instance_name}' 没有找到";
                return View("DailyReport", model);
            }

            if (model.CurrentLibraryCode == null)
            {
                model.ErrorInfo = $"model.CurrentLibraryCode 为 null";
                model.FillData(instance);
                return View("DailyReport", model);
            }

            if (model.NewReportType == null)
            {
                model.ErrorInfo = $"请输入拟创建的报表定义类型";
                model.FillData(instance);
                return View("DailyReport", model);
            }

            var ret = instance.GetDailyReportDefDom().AddDailyReportNewDef(model.CurrentLibraryCode,
                model.NewReportType);
            if (ret.Value == -1)
            {
                model.ErrorInfo = ret.ErrorInfo;
                model.FillData(instance);
                return View("DailyReport", model);
            }

            var type = model.NewReportType;

            model.FillData(instance);
            model.NewReportType = null;
            model.SuccessInfo = $"成功增添类型为 '{type}' 的报表定义";
            return View("DailyReport", model);
        }

        // 为每日报表的配置文件删除一个 report 元素
        [HttpPost]
        public IActionResult DeleteDailyReportDef
            (DailyReportViewModel model,
            string? libraryCode,
            string? type)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            var instance_name = (string?)this.Request.RouteValues["name"];
            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
            {
                model.ErrorInfo = $"实例名 '{instance_name}' 没有找到";
                return View("DailyReport", model);
            }
            if (libraryCode == null)
            {
                model.ErrorInfo = $"请指定拟删除的 library 元素的馆代码";
                model.FillData(instance);
                return View("DailyReport", model);
            }
            var ret = instance.GetDailyReportDefDom().DeleteDailyReportDef(libraryCode, type);
            if (ret.Value == -1)
            {
                model.ErrorInfo = ret.ErrorInfo;
                model.FillData(instance);
                return View("DailyReport", model);
            }
            model.FillData(instance);
            model.SuccessInfo = $"成功删除类型为 '{type}' 的报表定义";
            return View("DailyReport", model);
        }

        // 为每日报表的配置文件增全若干个 report 元素
        [HttpPost]
        public IActionResult AddDailyReportNewDefAll(DailyReportViewModel model)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            var instance_name = (string?)this.Request.RouteValues["name"];
            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
            {
                model.ErrorInfo = $"实例名 '{instance_name}' 没有找到";
                return View("DailyReport", model);
            }

            if (model.CurrentLibraryCode == null)
            {
                model.ErrorInfo = $"model.CurrentLibraryCode 为 null";
                model.FillData(instance);
                return View("DailyReport", model);
            }

            var ret = instance.GetDailyReportDefDom().AddDailyReportNewDefAll(model.CurrentLibraryCode);
            if (ret.Value == -1)
            {
                model.ErrorInfo = ret.ErrorInfo;
                model.FillData(instance);
                return View("DailyReport", model);
            }

            model.FillData(instance);
            model.NewReportType = null;
            if (ret.AddedTypes == null || ret.AddedTypes?.Count == 0)
                model.SuccessInfo = $"没有添加任何新的报表类型";
            else
                model.SuccessInfo = $"成功增添了以下 {ret.AddedTypes?.Count} 个报表类型: {string.Join(',', ret.AddedTypes)}";
            return View("DailyReport", model);
        }


        // EditDailyReportOneDef

        [HttpGet]
        public IActionResult EditDailyReportOneDef(string? libraryCode,
            string? reportType)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            var model = new DailyReportOneReportViewModel();

            var instance_name = (string?)this.Request.RouteValues["name"];
            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
            {
                model.ErrorInfo = $"实例名 '{instance_name}' 没有找到";
                return View("DailyReport", model);
            }

            // 通过 libraryCode 和 reportName 找到 ReportDef 定义对象
            var def = instance.GetDailyReportDefDom().GetReportDefs(libraryCode)?.Where(o=>o.Type == reportType).FirstOrDefault();
            if (def == null)
            {
                model.ErrorInfo = $"馆代码为 '{libraryCode}' 报表类型为 '{reportType}' 的报表定义没有找到";
                return View("EditDailyReportDef", model);
            }

            model = DailyReportOneReportViewModel.FromReportDef(def);
            model.LibraryCode = libraryCode;
            return View("EditDailyReportDef", model);
        }

        [HttpPost]
        public IActionResult EditDailyReportOneDef(DailyReportOneReportViewModel model)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            var instance_name = (string?)this.Request.RouteValues["name"];
            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
            {
                model.ErrorInfo = $"实例名 '{instance_name}' 没有找到";
                return View("EditDailyReportDef", model);
            }

            if (model.LibraryCode == null)
            {
                model.ErrorInfo = $"model.LibraryCode 为 null";
                return View("EditDailyReportDef", model);
            }

            var ret = instance
                .GetDailyReportDefDom()
                .UpdateDailyReportDef(model.LibraryCode,
                model.Type,
                model.ToReportDef());
            if (ret.Value == -1)
            {
                model.ErrorInfo = ret.ErrorInfo;
                return View("EditDailyReportDef", model);
            }

            return View("EditDailyReportDef", model);
        }

        #endregion

        #region 单个报表

        // 呈现“报表”页面
        [HttpGet]
        public IActionResult BuildReport()
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            var model = new ReportViewModel();

            var instance_name = (string?)this.Request.RouteValues["name"];
            var instance = ServerContext.Instances.FindInstance(instance_name);
            if (instance == null)
            {
                model.ErrorInfo = $"实例名 '{instance_name}' 没有找到";
                return View("Report", model);
            }

            model.InstanceName = instance.Name;
            model.Description = instance.Description;
            model.ReportType = "101";
            model.LibraryCode = "*";
            model.DateRange = "20210101-20221231";
            return View("Report", model);
        }

        // 执行“创建报表”命令
        [HttpPost]
        public IActionResult BuildReport(ReportViewModel model)
        {
            if (this.IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            if (ModelState.IsValid)
            {
                try
                {
                    var name = model.InstanceName;
                    if (name == null)
                    {
                        model.ErrorInfo = "实例名不允许为空";
                        return View("Report", model);
                    }

                    var instance = ServerContext.Instances.FindInstance(name);
                    if (instance == null)
                    {
                        model.ErrorInfo = $"实例名 '{name}' 没有找到";
                        return View("Report", model);
                    }

                    var ret = CreateReport(instance, model);
                    if (ret.Value == -1)
                    {
                        model.ErrorInfo = $"CreateReport() 出错: {ret.ErrorInfo}";
                        return View("Report", model);
                    }

                    model.HtmlContent = System.IO.File.ReadAllText(ret.strOutputHtmlFileName);
                    model.SuccessInfo = $"为实例 {name} 创建报表成功";
                }
                catch (Exception ex)
                {
                    model.ErrorInfo = ex.Message;
                }
            }

            return View("Report", model);
        }

        class CreateReportResult : NormalResult
        {
            public string strOutputFileName { get; set; }
            public string strOutputHtmlFileName { get; set; }
        }

        CreateReportResult CreateReport(
            Instance instance,
            ReportViewModel model)
        {
            var defDirectory = ServerContext.ReportDefDirectory;    // Path.Combine(Directory.GetCurrentDirectory(), "report_def");
            var defFileName = Path.Combine(defDirectory, $"{model.ReportType}.xml");

            ReportWriter writer = new ReportWriter();
            int nRet = writer.Initial(defFileName, out string strError);
            if (nRet == -1)
                return new CreateReportResult
                {
                    Value = -1,
                    ErrorInfo = strError
                };

            writer.Algorithm = model.ReportType;

            string strOutputFileName = Path.Combine(instance.DataDir, "~" + Guid.NewGuid().ToString() + ".rml");
            string strOutputHtmlFileName = Path.Combine(instance.DataDir, "~" + Guid.NewGuid().ToString() + ".html");

            // Hashtable param_table = new Hashtable();
            var param_table = StringUtil.ParseParameters(model.OtherParameters?.Replace("\r\n","\r").Replace("\n","\r"), '\r', ':');
            // libraryCode sortColumn dateRange
            param_table["libraryCode"] = model.LibraryCode;
            param_table["dateRange"] = model.DateRange;
            param_table["sortColumn"] = model.SortColumn;
            using (var context = new LibraryContext(instance.DbConfig))
            {
                Report.BuildReport(context,
                    param_table,
                    writer,
                    strOutputFileName);
            }

            if (System.IO.File.Exists(strOutputFileName) == false)
            {
                return new CreateReportResult
                {
                    Value = -1,
                    ErrorInfo = $"Report.BuildReport() 未能创建 .rml 文件",
                    strOutputFileName = strOutputFileName,
                };
            }

            // RML 格式转换为 HTML 文件
            // parameters:
            //      strCssTemplate  CSS 模板。里面 %columns% 代表各列的样式
            nRet = Report.RmlToHtml(strOutputFileName,
                strOutputHtmlFileName,
                "",
                out strError);
            if (nRet == -1)
                return new CreateReportResult
                {
                    Value = -1,
                    ErrorInfo = $"Report.RmlToHtml() 出错: {strError}",
                    strOutputFileName = strOutputFileName,
                };

            return new CreateReportResult
            {
                strOutputFileName = strOutputFileName,
                strOutputHtmlFileName = strOutputHtmlFileName,
            };
        }

        #endregion

        #region 数据库操作

        // 重新创建空白数据库
        void RecreateDatabase(Instance instance)
        {
            if (instance.DbConfig == null)
                throw new Exception($"实例 '{instance.Name}' 的 DbConfig 成员为空");

            using (var context = new LibraryContext(instance.DbConfig))
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }

            // 重置和每日报表任务有关的各种记忆
            instance.ResetDailyReportTask();
        }

        #endregion

        #region SSE

#if REMOVED
        // 将指定文件的内容不断发送给浏览器
        async Task SendMessageAsync(string fileName)
        {
            try
            {
                //Disable buffering on this response so it sends it out before it is done
                var responseFeature = HttpContext.Features.Get<IHttpResponseBodyFeature>();
                responseFeature?.DisableBuffering();

                using var s = System.IO.File.Open(fileName, 
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite);

                while (!HttpContext.RequestAborted.IsCancellationRequested)
                {
                REDO:
                    // 剩下的字节数
                    long rest = s.Length - s.Position;
                    if (rest == 0)
                    {
                        await Task.Delay(500, HttpContext.RequestAborted);
                        goto REDO;
                    }
                    if (rest < 0)
                    {
                        s.Seek(0, SeekOrigin.Begin);
                        goto REDO;
                    }
                    byte[] buffer = new byte[4096];
                    var length = Math.Min(rest, buffer.Length);
                    int count = await s.ReadAsync(buffer.AsMemory(0, (int)length), HttpContext.RequestAborted);
                    await Response.Body.WriteAsync(buffer.AsMemory(0, count));
                    await Response.Body.FlushAsync();
                }
            }
            catch(Exception ex)
            {
                await Response.WriteAsync($"SendMessageAsync() 出现异常: {ex.Message}\n\n");
                await Task.Delay(2000);
            }
        }
#endif


        #endregion

    }
}
