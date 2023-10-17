using System.Xml;
using Microsoft.AspNetCore.Mvc;
using dp2StatisServer.Data;
using System.Runtime.CompilerServices;

namespace dp2StatisServer.Controllers
{
    public class BaseController : Controller
    {
        internal readonly ILogger<HomeController> _logger;
        private readonly IInstanceRepository _instanceRepository;

        public BaseController(ILogger<HomeController> logger,
            IInstanceRepository instanceRepository)
        {
            _logger = logger;
            _instanceRepository = instanceRepository;
        }

        internal IInstanceRepository GetInstanceRepository()
        {
            return _instanceRepository;
        }

#if REMOVED


        internal bool IsSupervisor()
        {
            var userName = this.HttpContext.Session.GetString("UserName");
            var instanceName = this.HttpContext.Session.GetString("InstanceName");
            return (userName != null
                && string.IsNullOrEmpty(instanceName));
        }

        internal bool IsInstanceManager()
        {
            var userName = this.HttpContext.Session.GetString("UserName");
            var instanceName = this.HttpContext.Session.GetString("InstanceName");
            return (userName != null
                && string.IsNullOrEmpty(instanceName) == false);
        }

        internal string? CurrentUserName
        {
            get
            {
                return this.HttpContext.Session.GetString("UserName");
            }
        }
#endif

        #region 实现 IInstanceRepository

#if REMOVED
        internal Instance? _getItem(int id)
        {
            return _instanceRepository.GetItem(id);
        }

        internal IEnumerable<Item> _getItems(int pageNo, int pageSize)
        {
            return _itemRepository.GetItems(pageNo, pageSize);
        }

        internal int _getCount()
        {
            return _itemRepository.GetCount();
        }

        internal IEnumerable<Item?> _findItem(string? product_name, string mac)
        {
            return _itemRepository.FindItem(product_name, mac);
        }

        internal IEnumerable<Item> _searchItems(
        ItemQuery query,
        int pageNo,
        int pageSize,
        out int count)
        {
            return _itemRepository.SearchItems(query,
                pageNo,
                pageSize,
                out count);
        }

#endif

        #endregion
    }

    public static class ControllerBaseExtension
    {
        public static bool IsSupervisor(this ControllerBase controller)
        {
            return controller.HttpContext.IsSupervisor();
        }

        public static bool IsInstanceManager(this ControllerBase controller,
            string currentInstanceName)
        {
            return controller.IsInstanceManager(currentInstanceName);
        }

        public static bool IsInstanceManager(this ControllerBase controller)
        {
            var instance_name = (string?)controller.Request.RouteValues["name"];
            if (string.IsNullOrEmpty(instance_name))
                return false;   // TODO: 抛出异常?
            return controller.IsInstanceManager(instance_name);
        }

        public static bool IsInstanceManagerOrSupervisor(this ControllerBase controller,
            string currentInstanceName)
        {
            return controller.HttpContext.IsInstanceManagerOrSupervisor(currentInstanceName);
        }

        public static bool IsInstanceManagerOrSupervisor(this ControllerBase controller)
        {
            var instance_name = (string?)controller.Request.RouteValues["name"];
            if (string.IsNullOrEmpty(instance_name))
                return false;   // TODO: 抛出异常?
            return controller.HttpContext.IsInstanceManagerOrSupervisor(instance_name);
        }

        public static void RemoveSessionItem(this ControllerBase controller)
        {
            controller.HttpContext.RemoveSessionItem();
        }

        public static string? GetUserName(this ControllerBase controller)
        {
            return controller.HttpContext.GetUserName();
        }

        public static void SetUserName(this ControllerBase controller,
            string userName,
            string instanceName)
        {
            controller.HttpContext.SetUserName(userName,
                instanceName);
        }
    }

    public static class HttpContextExtension
    {
        public static bool IsSupervisor(this HttpContext context)
        {
            var userName = context.Session.GetString("UserName");
            var instanceName = context.Session.GetString("InstanceName");
            return (userName != null
                && string.IsNullOrEmpty(instanceName));
        }

        public static bool IsInstanceManager(this HttpContext context,
            string currentInstanceName)
        {
            var userName = context.Session.GetString("UserName");
            var instanceName = context.Session.GetString("InstanceName");
            return (userName != null
                && instanceName == currentInstanceName);
        }

        public static bool IsInstanceManagerOrSupervisor(this HttpContext context,
            string currentInstanceName)
        {
            var userName = context.Session.GetString("UserName");
            var instanceName = context.Session.GetString("InstanceName");
            return (userName != null || instanceName == currentInstanceName);
        }

        public static void RemoveSessionItem(this HttpContext context)
        {
            context.Session.Remove("UserName");
            context.Session.Remove("InstanceName");
        }

        public static string? GetUserName(this HttpContext context)
        {
            return context.Session.GetString("UserName");
        }

        public class UserInfo
        {
            public string? InstanceName { get; set; }
            public string? UserName { get; set; }
        }

        public static UserInfo GetUserInfo(this HttpContext context)
        {
            var userName = context.Session.GetString("UserName");
            var instanceName = context.Session.GetString("InstanceName");
            return new UserInfo
            {
                UserName = userName,
                InstanceName = instanceName
            };
        }

        public static void SetUserName(this HttpContext context,
            string userName,
            string instanceName)
        {
            context.Session.SetString("UserName", userName);
            context.Session.SetString("InstanceName", instanceName);
        }
    }
}
