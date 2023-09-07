using System.Xml;
using Microsoft.AspNetCore.Mvc;
using dp2StatisServer.Data;

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

}
