using DocumentFormat.OpenXml.EMMA;
using Npgsql;
using System.Reflection;
using System.Xml;

namespace dp2StatisServer.Data
{
    public class InstanceRepository : IInstanceRepository, IDisposable
    {
        // global.xml 文件路径
        string _xmlFileName = string.Empty;

        // 数据目录。根据 global.xml 中 根元素 dataDir 属性值定义
        // string _dataDirectory = string.Empty;

        public string? DataDirectory
        {
            get
            {
                return GetGlobalInfo()?.DataDirRoot;
            }
        }

        public InstanceRepository()
        {

            // Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _xmlFileName = Path.Combine(Directory.GetCurrentDirectory(), "global.xml");

            LoadXml();
            LoadInstances();
        }

        XmlDocument _dom = new XmlDocument();

        void LoadXml()
        {
            try
            {
                _dom.Load(_xmlFileName);
            }
            catch (FileNotFoundException ex)
            {
                _dom.LoadXml("<root />");
            }

            // 迫使初始化 dataDir
            // GetGlobalInfo();
        }

        void SaveXml()
        {
            _dom.Save(_xmlFileName);
        }

        // 获得全局信息
        public GlobalInfo? GetGlobalInfo()
        {
            var dataDir = _dom.DocumentElement?.GetAttribute("dataDir");
            GlobalInfo? info;
            if (string.IsNullOrEmpty(dataDir))
            {
                // 实例根目录设定为应用当前目录下的 instances 子目录
                var directory = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "instances");
                if (Directory.Exists(directory) == false)
                    Directory.CreateDirectory(directory);

                /*
                if (string.IsNullOrEmpty(_directory))
                    throw new Exception("_directory 为空");

                if (Directory.Exists(_directory) == false)
                    Directory.CreateDirectory(_directory);
                */
                info = new GlobalInfo(directory);
                // 保存到 global.xml 文件
                SetGlobalInfo(info);
            }
            else
                info = new GlobalInfo(dataDir);

            return info;
        }

        // 修改全局信息
        public void SetGlobalInfo(GlobalInfo info)
        {
            _dom.DocumentElement?.SetAttribute("dataDir", info.DataDirRoot);
            SaveXml();
        }

        List<Instance> _instances = new List<Instance>();

        public IEnumerable<Instance> GetInstances()
        {
            return new List<Instance>(_instances);
        }

        void LoadInstances()
        {
            this.Dispose();
            // _instances.Clear();

            // 遍历 root 目录的下级目录
            var root_dir = GetGlobalInfo()?.DataDirRoot;
            if (root_dir != null)
            {
                var di = new DirectoryInfo(root_dir);
                var dirs = di.GetDirectories();
                foreach (var data_dir in dirs)
                {
                    var instance = LoadOneInstance(data_dir.FullName);
                    this._instances.Add(instance);
                }
            }
        }

        // 从数据目录装载获得一个实例
        Instance LoadOneInstance(string data_dir)
        {
            Instance instance = new Instance();
            instance.DataDir = data_dir;
            instance.LoadConfigFile();
            instance._getData();

            return instance;
        }

        // 寻找一个实例
        // parameters:
        //      name    实例名。如果以空字符串调用本函数，则返回第一个实例
        public Instance? FindInstance(string name)
        {
            if (string.IsNullOrEmpty(name)) 
                return _instances.FirstOrDefault();
            return _instances.Where(o => o.Name == name).ToList().FirstOrDefault();
        }

        public void DeleteInstance(string name,
            string? adminPassword)
        {
            var instance = FindInstance(name);
            if (instance == null)
                throw new Exception($"实例 '{name}' 没有找到");

            // 删除 Pgsql 用户
            var userName = instance.DbConfig?.UserName;
            if (string.IsNullOrEmpty(userName) == false)
            {
                var error = _deletePgsqlUser(adminPassword,
        userName,
        out string _);
                if (error != null)
                {
                    throw new Exception($"尝试删除 {userName} 用户失败: {error}");
                }
            }

            // 删除数据目录
            instance.DeleteDataDir();
            _instances.Remove(instance);
            ServerContext.Initialize(true);
        }


        // 修改实例信息
        public void SetInstance(Instance instance)
        {
            instance._setData();
            instance.SaveConfigFile();
            ServerContext.Initialize(true);
        }

        // 创建一个新的实例
        // parameters:
        //      name    实例名。英文字符串
        //      adminPassword Pgsql 的 postgres 用户密码
        //      instanceUserPassword 拟创建的实例用户(Pgsql用户)的密码
        public Instance? CreateInstance(string name,
            string description,
            string replicateTime,
            string appServerUrl,
            string appServerUserName,
            string appServerPassword,
            string adminPassword,
            string instanceUserPassword)
        {
            var instance = new Instance();

            if (string.IsNullOrEmpty(DataDirectory))
                throw new Exception("DataDirectory 尚未初始化");

            // TODO: 实例名查重

            // instance 子目录
            // TODO: 实例子目录路径查重
            var dir = Path.Combine(DataDirectory, name);
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
            instance.DataDir = dir;
            instance.Name = name;
            instance.Description = description;
            instance.ReplicateTime = replicateTime;

            if (string.IsNullOrEmpty(instanceUserPassword))
                throw new ArgumentException($"instanceUserPassword 参数值不允许为空", nameof(instanceUserPassword));

            string instanceUserName = "s_" + name;

        REDO_SETUP:
            var error = _setupPgsqlUser(adminPassword,
                instanceUserName,
                instanceUserPassword,
                "",
                out string error_code);
            if (error_code == "alreadyExist")
            {
                /*
                GetItemRepository().RecreateDatabase("ensureDeleted");
                GetJournalRepository().RecreateDatabase("ensureDeleted");
                */

                error = _deletePgsqlUser(adminPassword,
                    instanceUserName,
                    out string _);
                if (error != null)
                {
                    throw new Exception($"尝试删除 xxx 用户失败: {error}");
                }
                goto REDO_SETUP;
            }

            if (error != null)
                throw new Exception(error);

            instance.SetDbConfig("localhost",
                instanceUserName,
                instanceUserPassword,
                instanceUserName);

            instance.AppServerUrl = appServerUrl;
            instance.AppServerUserName = appServerUserName;
            instance.AppServerPassword = appServerPassword;

            _instances.Add(instance);
            instance._setData();
            // 写入实例 xml 配置文件
            instance.SaveConfigFile();
            ServerContext.Initialize(true);
            return instance;
        }

        #region Pgsql

        // 2023/8/24
        internal string? _setupPgsqlUser(
            string? adminPassword,
            string userName,
            string? password,
            string style,
            out string error_code)
        {
            error_code = "";

            string strSqlServerName = "localhost";
            var adminUserName = "postgres"; // Console.ReadLine();

            string strConnection = $"Host={strSqlServerName};Username={adminUserName};Password={adminPassword};";

            try
            {
                using (var connection = new NpgsqlConnection(strConnection))
                {
                    try
                    {
                        connection.Open();
                        string strCommand = $"CREATE USER \"{userName}\" PASSWORD '{password}';"
                            + $"ALTER USER \"{userName}\" CREATEDB;";
                        using (var command = new NpgsqlCommand(strCommand, connection))
                        {
                            var count = command.ExecuteNonQuery();
                        }

                        return null;
                    }
                    catch (PostgresException ex)
                    {
                        // https://www.postgresql.org/docs/current/errcodes-appendix.html
                        // 42710	duplicate_object
                        if (ex.SqlState == "42710")
                        {
                            error_code = "alreadyExist";
                            return $"用户 '{userName}' 已经存在";
                            // TODO: 验证用户密码？
                        }
                        return $"创建用户 {userName} 出错: {ex.Message}";
                    }
                    catch (NpgsqlException sqlEx)
                    {
                        return $"创建用户 {userName} 出错: {sqlEx.Message}";
                        // int nError = (int)sqlEx.ErrorCode;
                    }
                    catch (Exception ex)
                    {
                        return $"创建用户 {userName} 出错: {ex.Message}";
                    }
                }
            }
            catch (Exception ex)
            {
                return "建立连接出错：" + ex.Message + " 类型:" + ex.GetType().ToString();
            }
        }

        /* 手动删除 sncenter 用户的过程:
         * 先用 postgres 用户登录。
postgres=# drop user sncenter;
错误:  无法删除"sncenter"因为有其它对象倚赖它
描述:  数据库 snjournal的属主
数据库 snitem的属主
在数据库 snjournal中的2个对象
在数据库 snitem中的3个对象
postgres=# drop owned by sncenter cascade;
        // 注：这样子是删不掉的，因为当前登录身份还是 postgres，不是 sncenter
DROP OWNED
postgres=# drop user sncenter;
错误:  无法删除"sncenter"因为有其它对象倚赖它
描述:  数据库 snjournal的属主
数据库 snitem的属主
在数据库 snjournal中的2个对象
在数据库 snitem中的3个对象
postgres=# REASSIGN OWNED BY sncenter to postgres;
REASSIGN OWNED
postgres=# drop database snitem;
DROP DATABASE
postgres=# drop database snjournal;
DROP DATABASE
postgres=# drop user sncenter;
DROP ROLE
postgres=#
        * */
        internal string? _deletePgsqlUser(
    string? adminPassword,
    string userName,
    out string error_code)
        {
            error_code = "";

            string strSqlServerName = "localhost";
            var adminUserName = "postgres"; // Console.ReadLine();

            string strConnection = $"Host={strSqlServerName};Username={adminUserName};Password={adminPassword};";

            try
            {
                using (var connection = new NpgsqlConnection(strConnection))
                {
                    try
                    {
                        connection.Open();
                        string strCommand =
                            $"REASSIGN OWNED BY sncenter to postgres;"
                            + $"drop database if exists snitem;"
                            + $"drop database if exists snjournal;"
                            + $"DROP OWNED BY \"{userName}\";"
                            + $"DROP USER \"{userName}\";";
                        using (var command = new NpgsqlCommand(strCommand, connection))
                        {
                            var count = command.ExecuteNonQuery();
                        }

                        return null;
                    }
                    catch (PostgresException ex)
                    {
                        // https://www.postgresql.org/docs/current/errcodes-appendix.html
                        // 42710	undefined_object
                        if (ex.SqlState == "42704")
                        {
                            error_code = "notExist";
                            return $"用户 {userName} 不存在";
                        }
                        // Npgsql.PostgresException:“2BP01: 无法删除"sncenter"因为有其它对象倚赖它
                        return $"删除用户 {userName} 出错: {ex.Message}";
                    }
                    catch (NpgsqlException sqlEx)
                    {
                        return $"删除用户 {userName} 出错: {sqlEx.Message}";
                        // int nError = (int)sqlEx.ErrorCode;
                    }
                    catch (Exception ex)
                    {
                        return $"删除用户 {userName} 出错: {ex.Message}";
                    }
                }
            }
            catch (Exception ex)
            {
                return "建立连接出错：" + ex.Message + " 类型:" + ex.GetType().ToString();
            }
        }

        public void Dispose()
        {
            if (_instances.Count > 0)
            {
                foreach (var instance in _instances)
                {
                    instance.CancelReplication();
                }
                _instances.Clear();
            }
        }


        #endregion
    }
}
