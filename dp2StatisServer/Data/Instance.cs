using System.Xml;

using DigitalPlatform;
using DigitalPlatform.LibraryClientOpenApi;
using DigitalPlatform.LibraryServer.Reporting;
using DigitalPlatform.Xml;

namespace dp2StatisServer.Data
{
    // 一个图书馆实例
    public class Instance
    {
        // 实例名字
        public string? Name { get; set; }

        // 描述
        public string? Description { get; set; }

        // 同步时间。为空字符串或者一个每日时刻(例如 "12:00")
        public string? ReplicateTime { get; set; }

        // 数据目录
        public string? DataDir { get; set; } = string.Empty;

        // dp2library URL
        public string? AppServerUrl { get; set; }
        public string? AppServerUserName { get; internal set; }
        public string? AppServerPassword { get; internal set; }

        public DatabaseConfig? DbConfig { get; set; }

        XmlDocument? _dom = null;

        // 删除 Pgsql 用户，删除实例数据目录
        public void DeleteDataDir()
        {
            if (string.IsNullOrEmpty(DataDir) == false)
            {
                Directory.Delete(DataDir, true);
                DataDir = null;
            }
        }

        public void SetDbConfig(
            string serverName,
            string userName,
            string password,
            string databaseName)
        {
            if (this.DbConfig == null)
                this.DbConfig = new DatabaseConfig();
            this.DbConfig.ServerName = serverName;
            this.DbConfig.UserName = userName;
            this.DbConfig.Password = password;
            this.DbConfig.DatabaseName = databaseName;
        }

        internal void LoadConfigFile()
        {
            if (string.IsNullOrEmpty(this.DataDir))
                throw new ArgumentException($"this.DataDir 不应为空");

            string fileName = Path.Combine(this.DataDir, "config.xml");
            _dom = new XmlDocument();
            _dom.Load(fileName);

            if (File.Exists(TaskFileName))
            {
                this._taskDom = new XmlDocument();
                this._taskDom.Load(TaskFileName);
            }
        }

        internal void SaveConfigFile()
        {
            if (string.IsNullOrEmpty(this.DataDir))
                throw new ArgumentException($"this.DataDir 不应为空");

            if (_dom == null)
            {
                _dom = new XmlDocument();
                _dom.LoadXml("<instance />");
            }
            string fileName = Path.Combine(this.DataDir, "config.xml");
            _dom.Save(fileName);

            SetTaskDom(this._taskDom);
        }

        // 把 _dom 中的数据装入类成员
        internal void _getData()
        {
            if (_dom == null)
                throw new ArgumentException("_dom 不应为 null");

            this.Name = _dom.DocumentElement.GetElementText("name");
            this.Description = _dom.DocumentElement.GetElementText("description");
            this.ReplicateTime = _dom.DocumentElement.GetElementText("replicateTime");

            //this.DataDir = _dom.DocumentElement.GetElementText("dataDir");

            {
                this.AppServerUrl = _dom.GetElementAttribute("appServer", "url");
                this.AppServerUserName = _dom.GetElementAttribute("appServer", "userName");
                this.AppServerPassword = _dom.GetElementAttribute("appServer", "password");
            }

            this.DbConfig = GetDatabaseConfig(_dom);
        }

        internal void _verifyData()
        {
            if (this.ReplicateTime != null)
            {
                var ret = PerdayTime.ParseTime(this.ReplicateTime);
                if (ret.Value == -1)
                    throw new Exception($"同步时间值 '{this.ReplicateTime}' 不合法:" + ret.ErrorInfo);
            }
        }

        // 把类成员中的数据写入 _dom
        internal void _setData()
        {
            _verifyData();

            if (_dom == null)
            {
                _dom = new XmlDocument();
                _dom.LoadXml("<instance />");
            }

            if (_dom.DocumentElement == null)
                throw new ArgumentException("_dom.DocumentElement 不应为 null");

            _dom.SetElementText("name", this.Name);
            _dom.SetElementText("description", this.Description);
            _dom.SetElementText("replicateTime", this.ReplicateTime);

            //_dom.SetElementText("dataDir",this.DataDir);
            _dom.SetElementText("appServerUrl", this.AppServerUrl);

            {
                var element = _dom.EnsureElement("appServer");
                element.SetAttribute("url", this.AppServerUrl);
                element.SetAttribute("userName", this.AppServerUserName);
                element.SetAttribute("password", this.AppServerPassword);
            }

            SetDatabaseConfig(_dom, this.DbConfig);
        }

        static DatabaseConfig GetDatabaseConfig(XmlDocument dom)
        {
            if (dom == null || dom.DocumentElement == null)
                return new DatabaseConfig();
            var database = dom.DocumentElement.SelectSingleNode("database") as XmlElement;
            if (database == null)
                return new DatabaseConfig();
            var config = new DatabaseConfig();
            config.ServerName = database.GetAttribute("serverName");
            config.UserName = database.GetAttribute("userName");
            config.Password = database.GetAttribute("password");
            config.DatabaseName = database.GetAttribute("databaseName");
            return config;
        }

        static void SetDatabaseConfig(XmlDocument dom,
            DatabaseConfig? config)
        {
            if (dom.DocumentElement == null)
                dom.LoadXml("<instance />");
            var database = dom.DocumentElement.SelectSingleNode("database") as XmlElement;
            if (database == null)
            {
                database = dom.CreateElement("database");
                dom.DocumentElement.AppendChild(database);
            }

            if (config == null)
            {
                database.RemoveAllAttributes();
                return;
            }
            database.SetAttribute("serverName", config.ServerName);
            database.SetAttribute("userName", config.UserName);
            database.SetAttribute("password", config.Password);
            database.SetAttribute("databaseName", config.DatabaseName);
        }

        #region LibraryChannel

        // 主要的通道池，用于当前服务器
        LibraryChannelPool _channelPool = new LibraryChannelPool();

        List<LibraryChannel> _channelList = new List<LibraryChannel>();

        public Instance()
        {
            _channelPool.BeforeLogin += _channelPool_BeforeLogin;
        }

        private void _channelPool_BeforeLogin(object sender, BeforeLoginEventArgs e)
        {
            if (e.FirstTry == true)
            {
                // string strPhoneNumber = "";

                {
                    e.UserName = this.AppServerUserName;
                    e.Password = this.AppServerPassword;

                    bool bIsReader = false;

                    string strLocation = "dp2Statis";

                    e.Parameters = "location=" + strLocation;
                    if (bIsReader == true)
                        e.Parameters += ",type=reader";
                }

                // 2014/9/13
                // e.Parameters += ",mac=" + StringUtil.MakePathList(SerialCodeForm.GetMacAddress(), "|");

                e.Parameters += ",client=testreporting|" + "0.01";

                if (String.IsNullOrEmpty(e.UserName) == false)
                    return; // 立即返回, 以便作第一次 不出现 对话框的自动登录
                else
                {
                    e.ErrorInfo = "尚未配置 dp2library 服务器用户名";
                    e.Cancel = true;
                }
            }

            // e.ErrorInfo = "尚未配置 dp2library 服务器用户名";
            e.Cancel = true;
        }

        public void AbortAllChannel()
        {
            foreach (LibraryChannel channel in _channelList)
            {
                if (channel != null)
                    channel.Abort();
            }
        }

        // parameters:
        //      style    风格。如果为 GUI，表示会自动添加 Idle 事件，并在其中执行 Application.DoEvents
        public LibraryChannel GetChannel()
        {
            string strServerUrl = this.AppServerUrl;

            string strUserName = this.AppServerUserName;

            LibraryChannel channel = this._channelPool.GetChannel(strServerUrl, strUserName);
            _channelList.Add(channel);
            // TODO: 检查数组是否溢出
            return channel;
        }

        public void ReturnChannel(LibraryChannel channel)
        {
            this._channelPool.ReturnChannel(channel);
            _channelList.Remove(channel);
        }

        #endregion

        #region Replication

        // TODO: 注意 app down 的时候保存 _taskDom 到文件
        internal XmlDocument? _taskDom = null;

        internal string? _progressFileName = null;

        internal Task<NormalResult>? _replicationTask = null;
        internal CancellationTokenSource? _cancelReplication = null;

        public void CancelReplication()
        {
            _cancelReplication?.Cancel();
        }

        // parameters:
        //      first   是否为首次运行?
        public Task<NormalResult> BeginReplication(bool first,
            string? info = null)
        {
            if (this._replicationTask != null &&
                (
                this._replicationTask.Status == TaskStatus.Running
                || this._replicationTask.Status == TaskStatus.RanToCompletion
                ))
                throw new Exception("同步任务先前已经启动，无法重复启动");

            if (this._cancelReplication != null)
                this._cancelReplication.Dispose();
            this._cancelReplication = new CancellationTokenSource();
            var token = this._cancelReplication.Token;

            this._replicationTask = Task<NormalResult>.Factory.StartNew(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(this._progressFileName) == false)
                    {
                        // System.IO.File.Delete(instance._progressFileName);
                    }
                    else
                        this._progressFileName = Path.Combine(DataDir, "replication_progress.txt");

                    string fileName = this._progressFileName;

                    using var s = System.IO.File.Open(fileName,
                        FileMode.OpenOrCreate,
                        FileAccess.ReadWrite,
                        FileShare.ReadWrite);
                    s.SetLength(0);
                    // s.Seek(0, SeekOrigin.End);
                    using var writer = new StreamWriter(s, System.Text.Encoding.UTF8);

                    token.Register(() =>
                    {
                        OutputHistory(writer, "同步过程已经被终止。");
                        // instance._replicationTask?.Dispose();
                        this._replicationTask = null;
                    });

                    if (string.IsNullOrEmpty(info) == false)
                        OutputHistory(writer, info);

                    Replication replication = new Replication();
                    LibraryChannel channel = this.GetChannel();
                    try
                    {
                        OutputHistory(writer, "replication.Initialize()");

                        int nRet = replication.Initialize(channel,
                            out string strError);
                        if (nRet == -1)
                        {
                            strError = $"同步过程 Initialize() 出错: {strError}";
                            goto ERROR1;
                        }

                        XmlDocument? task_dom = this._taskDom;

                        if (first)
                        {
                            OutputHistory(writer, "replication.BuildFirstPlan()");

                            nRet = replication.BuildFirstPlan("*",
                                channel,
                                (message) =>
                                {
                                    OutputHistory(writer, message);
                                },
                                token,
                                out task_dom,
                                out strError);
                            SetTaskDom(task_dom);
                            if (nRet == -1)
                            {
                                strError = $"同步过程 BuildFirstPlan() 出错: {strError}";
                                goto ERROR1;
                            }
                        }

                        if (task_dom == null)
                        {
                            if (first)
                            {
                                strError = $"同步过程出错: 首次同步时 _taskDom 为 null";
                                goto ERROR1;
                            }
                            else
                            {
                                strError = $"同步过程出错: 无法直接进行继续同步。需要先手动启动首次同步";
                                goto ERROR1;
                            }
                        }

                        OutputHistory(writer, "replication.RunFirstPlan()");

                        nRet = replication.RunFirstPlan(
                            this.DbConfig,
                            channel,
                            ref task_dom,
                            (message) =>
                            {
                                OutputHistory(writer, message);
                            },
                            token,
                            out strError);
                        SetTaskDom(task_dom);
                        if (nRet == -1)
                        {
                            strError = $"同步过程 RunFirstPlan() 出错: {strError}";
                            goto ERROR1;
                        }

                        OutputHistory(writer, "同步过程成功完成。");
                        return new NormalResult();
                    ERROR1:
                        OutputHistory(writer, strError);
                        return new NormalResult
                        {
                            Value = -1,
                            ErrorInfo = strError
                        };
                    }
                    finally
                    {
                        this.ReturnChannel(channel);
                    }
                }
                finally
                {
                    this._replicationTask = null;
                }
            },
token,
TaskCreationOptions.LongRunning,
TaskScheduler.Default);

            return this._replicationTask;
        }

        string TaskFileName
        {
            get
            {
                if (string.IsNullOrEmpty(this.DataDir))
                    throw new ArgumentNullException("this.DataDir 为空");

                return Path.Combine(this.DataDir, "replication_task.xml");
            }
        }

        // 设置和保存 task_dom
        void SetTaskDom(XmlDocument? task_dom)
        {
            this._taskDom = task_dom;
            this._taskDom?.Save(TaskFileName);
        }

        // TaskDom 是否已经初始化
        public bool HasTaskDom
        {
            get
            {
                return this._taskDom != null;
            }
        }

        /*
        XmlDocument? GetTaskDom()
        {
            return this._taskDom;
        }
        */

        static void OutputHistory(StreamWriter writer, string m)
        {
            writer.Write(DateTime.Now.ToLongTimeString() + " " + m + "\n");
            writer.Flush();
        }

        public async Task SendMessageAsync(
            Delegate_writeAsync func,
            CancellationToken token)
        {
            try
            {
                // 等待文件名准备就绪
                while (!token.IsCancellationRequested)
                {
                    if (string.IsNullOrEmpty(this._progressFileName) == false)
                        break;
                    await Task.Delay(500, token);
                }

                await SendMessageAsync(this._progressFileName,
                    func,
                    token);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }

        public delegate Task Delegate_writeAsync(string text);

        async Task SendMessageAsync(string fileName,
            Delegate_writeAsync func,
            CancellationToken token)
        {
            try
            {
                using var s = System.IO.File.Open(fileName,
                    FileMode.Open,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite);
                using var reader = new StreamReader(s, System.Text.Encoding.UTF8);

                while (!token.IsCancellationRequested)
                {
                REDO:
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        await Task.Delay(500, token);
                        goto REDO;
                    }
                    /*
                    if (rest < 0)
                    {
                        s.Seek(0, SeekOrigin.Begin);
                        goto REDO;
                    }
                    */
                    await func("data:" + line + "\n\n");
                    // await Response.WriteAsync("data:" + line + "\n\n");
                }
            }
            catch (Exception ex)
            {
                await func($"SendMessageAsync() 出现异常: {ex.Message}\n\n");
                // await Response.WriteAsync($"SendMessageAsync() 出现异常: {ex.Message}\n\n");
                await Task.Delay(2000);
            }
        }



        #endregion

    }

}
