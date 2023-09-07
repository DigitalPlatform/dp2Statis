using System;
using System.IO;
using System.Xml;

using Microsoft.AspNetCore.Mvc;

using dp2StatisServer.Data;
using dp2StatisServer.ViewModels;
using DigitalPlatform.LibraryServer.Reporting;
using DigitalPlatform.LibraryClientOpenApi;
using DigitalPlatform;

namespace dp2StatisServer.Controllers
{
    [Route("{controller=Instance}/{name=}/{action=Index}")]
    public class InstanceController : Controller
    {
        public IActionResult Index()
        {
            var instance_name = (string?)this.Request.RouteValues["name"];
            if (string.IsNullOrEmpty(instance_name))
                return View(NewIndexModel());

            if (IsInstanceManagerOrSupervisor() == false)
                return RedirectToLogin();

            return View(NewInstanceModel(instance_name));
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

        public IActionResult Replication()
        {
            if (IsInstanceManagerOrSupervisor() == false)
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

        internal bool IsInstanceManager()
        {
            var userName = this.HttpContext.Session.GetString("UserName");
            var instanceName = this.HttpContext.Session.GetString("InstanceName");
            return (userName != null
                && string.IsNullOrEmpty(instanceName) == false);
        }

        internal bool IsInstanceManagerOrSupervisor()
        {
            var userName = this.HttpContext.Session.GetString("UserName");
            // var instanceName = this.HttpContext.Session.GetString("InstanceName");
            return (userName != null);
        }

        [HttpPost]
        public IActionResult RecreateDatabase(OneInstanceViewModel model)
        {
            if (IsInstanceManagerOrSupervisor() == false)
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


        [HttpPost]
        public IActionResult ChangeReplicateTime(OneInstanceViewModel model)
        {
            if (IsInstanceManagerOrSupervisor() == false)
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
            if (IsInstanceManagerOrSupervisor() == false)
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
            if (IsInstanceManagerOrSupervisor() == false)
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
