using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DigitalPlatform.LibraryClientOpenApi
{
    public class dp2libraryService
    {
        /// <summary>
        /// 按需登录,当前通道的登录前事件
        /// </summary>
        public event BeforeLoginEventHandle BeforeLogin;

        // private readonly HttpClient _httpClient;

        private readonly ILogger _logger;
        // private readonly IBusinessLayer _business;
        private IHttpClientFactory _httpFactory { get; set; }
        public dp2libraryService(ILogger<dp2libraryService> logger, /*IBusinessLayer business,*/
            IHttpClientFactory httpFactory)
        {
            _logger = logger;
            // _business = business;
            _httpFactory = httpFactory;
        }

        public string? Url { get; set; }

        public async Task<LoginResponse> LoginAsync(LoginRequest body)
        {
            var httpClient = _httpFactory.CreateClient();
            dp2libraryClient client = new dp2libraryClient(Url, httpClient);
            return await client.LoginAsync(body);
        }

        public async Task<GetRecordResponse> GetRecordAsync(GetRecordRequest body)
        {
            var httpClient = _httpFactory.CreateClient();
            dp2libraryClient client = new dp2libraryClient(Url, httpClient);

            while (true)
            {
                var result = await client.GetRecordAsync(body);
                if (await CheckResult(result.GetRecordResult) == false)
                    return result;
            }
        }

        // return:
        //      false   不用重做
        //      true    需要重做
        async Task<bool> CheckResult(LibraryServerResult result)
        {
            if (result.Value == -1
    && (int)result.ErrorCode == (int)LibraryErrorCode.NotLogin)
            {
                // return.Value:
                //      -1  出错
                //      1   登录成功
                var retry_result = await TryLoginAsync(result.ErrorInfo);
                if (retry_result.Value != 1)
                {
                    result.ErrorInfo = retry_result.ErrorInfo;
                    return false;
                }
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// 清零重新登录次数
        /// </summary>
        void ClearRedoCount()
        {
            this.m_nRedoCount = 0;
        }

        // 重登录次数
        int _loginCount = 0;

        int m_nRedoCount = 0;   // MessageSecurityException以后重试的次数

        // return.Value:
        //      -1  出错
        //      1   登录成功
        public async Task<NormalResult> TryLoginAsync(string strError)
        {
            this.ClearRedoCount();

            if (this.BeforeLogin != null)
            {
                BeforeLoginEventArgs ea = new BeforeLoginEventArgs();
                ea.LibraryServerUrl = this.Url;
                ea.FirstTry = true;
                ea.ErrorInfo = strError;

            REDOLOGIN:
                this.BeforeLogin(this, ea);

                if (ea.Cancel == true)
                {
                    if (String.IsNullOrEmpty(ea.ErrorInfo) == true)
                        strError = "用户放弃登录";
                    else
                        strError = ea.ErrorInfo;
                    return new NormalResult
                    {
                        Value = -1,
                        ErrorInfo = strError
                    };
                }

                if (ea.Failed == true)
                {
                    strError = ea.ErrorInfo;
                    return new NormalResult
                    {
                        Value = -1,
                        ErrorInfo = strError
                    };
                }

                // 2006/12/30
                if (this.Url != ea.LibraryServerUrl)
                {
                    // this.Close();   // 迫使重新构造m_ws 2011/11/22
                    this.Url = ea.LibraryServerUrl;
                }

                string strMessage = "";
                if (ea.FirstTry == true)
                    strMessage = strError;

                if (_loginCount > 100)
                {
                    strError = "重新登录次数太多，超过 100 次，请检查登录 API 是否出现了逻辑问题";
                    _loginCount = 0;    // 重新开始计算
                    return new NormalResult
                    {
                        Value = -1,
                        ErrorInfo = strError
                    };
                }
                _loginCount++;

                var result = await LoginAsync(new LoginRequest { 
                StrUserName = ea.UserName,
                StrPassword = ea.Password,
                StrParameters = ea.Parameters,
                });

                if (result.LoginResult.Value == -1 || result.LoginResult.Value == 0)
                {
                    if (String.IsNullOrEmpty(strMessage) == false)
                        ea.ErrorInfo = strMessage + "\r\n\r\n首次自动登录报错: ";
                    else
                        ea.ErrorInfo = "";
                    ea.ErrorInfo += result.LoginResult.ErrorInfo;
                    ea.FirstTry = false;
                    ea.LoginFailCondition = LoginFailCondition.PasswordError;
                    goto REDOLOGIN;
                }

                /*
                // this.m_nRedoCount = 0;
                if (this.AfterLogin != null)
                {
                    AfterLoginEventArgs e1 = new AfterLoginEventArgs();
                    this.AfterLogin(this, e1);
                    if (string.IsNullOrEmpty(e1.ErrorInfo) == false)
                    {
                        strError = e1.ErrorInfo;
                        return -1;
                    }
                }
                 */
                // 登录成功,可以重做API功能了
                return new NormalResult
                {
                    Value = 1,
                    ErrorInfo = strError
                };
            }

            return new NormalResult
            {
                Value = -1,
                ErrorInfo = strError
            };
        }

#if NO

        public dp2libraryService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri(Url);

            /*
            // using Microsoft.Net.Http.Headers;
            // The GitHub API requires two headers.
            _httpClient.DefaultRequestHeaders.Add(
                HeaderNames.Accept, "application/vnd.github.v3+json");
            _httpClient.DefaultRequestHeaders.Add(
                HeaderNames.UserAgent, "HttpRequestsSample");
            */
        }

        /*
        public async Task<IEnumerable<GitHubBranch>?> GetAspNetCoreDocsBranchesAsync() =>
            await _httpClient.GetFromJsonAsync<IEnumerable<GitHubBranch>>(
                "repos/dotnet/AspNetCore.Docs/branches");
        */

        public async Task<LoginResponse> Login(LoginRequest body)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            return await client.LoginAsync(body);
        }
#endif
    }

    /// <summary>
    /// 登录失败的原因
    /// </summary>
    public enum LoginFailCondition
    {
        /// <summary>
        /// 没有出错
        /// </summary>
        None = 0,   // 没有出错
        /// <summary>
        /// 一般错误
        /// </summary>
        NormalError = 1,    // 一般错误
        /// <summary>
        /// 密码不正确
        /// </summary>
        PasswordError = 2,  // 密码不正确
    }

    /// <summary>
    /// 登录前的事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    public delegate void BeforeLoginEventHandle(object sender,
    BeforeLoginEventArgs e);

    /// <summary>
    /// 登录前时间的参数
    /// </summary>
    public class BeforeLoginEventArgs : EventArgs
    {
        /// <summary>
        /// [in] 是否为第一次触发
        /// </summary>
        public bool FirstTry = true;    // [in] 是否为第一次触发
        /// <summary>
        /// [in] 图书馆应用服务器 URL
        /// </summary>
        public string LibraryServerUrl = "";    // [in] 图书馆应用服务器URL

        /// <summary>
        /// [out] 用户名
        /// </summary>
        public string UserName = "";    // [out] 用户名
        /// <summary>
        /// [out] 密码
        /// </summary>
        public string Password = "";    // [out] 密码
        /// <summary>
        /// [out] 工作台号
        /// </summary>
        public string Parameters = "";    // [out] 工作台号

        /// <summary>
        /// [out] 事件调用是否失败
        /// </summary>
        public bool Failed = false; // [out] 事件调用是否失败
        /// <summary>
        /// [out] 事件调用是否被放弃
        /// </summary>
        public bool Cancel = false; // [out] 事件调用是否被放弃

        /// <summary>
        /// [in, out] 事件调用错误信息
        /// </summary>
        public string ErrorInfo = "";   // [in, out] 事件调用错误信息

        /// <summary>
        /// [in, out] 前次登录失败的原因，本次登录失败的原因
        /// </summary>
        public LoginFailCondition LoginFailCondition = LoginFailCondition.NormalError;
    }

    public class NormalResult
    {
        public int Value { get; set; }
        public string? ErrorInfo { get; set; }
        public string? ErrorCode { get; set; }

        public NormalResult(NormalResult result)
        {
            this.Value = result.Value;
            this.ErrorInfo = result.ErrorInfo;
            this.ErrorCode = result.ErrorCode;
        }

        public NormalResult(int value, string error)
        {
            this.Value = value;
            this.ErrorInfo = error;
        }

        public NormalResult()
        {

        }

        public override string ToString()
        {
            return $"Value={Value},ErrorInfo={ErrorInfo},ErrorCode={ErrorCode}";
        }
    }

}
