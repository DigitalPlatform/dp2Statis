using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DigitalPlatform.LibraryClientOpenApi
{


    public class LibraryChannel
    {
        HttpClient _httpClient = new HttpClient();

        /// dp2Library 服务器的 URL
        public string? Url { get; set; }
        public string? UserName { get; set; }

        /*
        /// <summary>
        /// 当前通道所使用的 HTTP Cookies
        /// </summary>
        private CookieContainer Cookies = new System.Net.CookieContainer();
        */

        /// <summary>
        /// 按需登录,当前通道的登录前事件
        /// </summary>
        public event BeforeLoginEventHandle? BeforeLogin;

        public event AfterLoginEventHandle? AfterLogin;


        TimeSpan _timeout = TimeSpan.Zero;

        public TimeSpan Timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                _timeout = value;
            }
        }

        public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(120);

        public LibraryChannel()
        {
            _httpClient.Timeout = DefaultTimeout;
        }

        public void Close()
        {
            _ = LogoutAsync();
        }

        public void Abort()
        {
            TriggerStop();
        }

        public string? LibraryCodeList { get; set; }
        public string? Rights { get; set; }

        public async Task<LoginResponse> LoginAsync(LoginRequest body,
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);

            using var pair = new TokenPair(_timeout, token);
            /*
            using var timeout_source = new CancellationTokenSource(_timeout);
            using var linked_source = CancellationTokenSource.CreateLinkedTokenSource(timeout_source.Token, token);
            */
            var result = await client.LoginAsync(body, pair.LinkedToken);
            LibraryCodeList = result.StrLibraryCode;
            Rights = result.StrRights;
            return result;
        }

        class TokenPair : IDisposable
        {
            public CancellationTokenSource? Timeout { get; set; }
            public CancellationTokenSource? Linked { get; set; }

            public CancellationToken LinkedToken
            {
                get
                {
                    if (Linked != null)
                        return Linked.Token;
                    return default;
                }
            }

            public void Dispose()
            {
                if (Timeout != null)
                {
                    Timeout.Dispose();
                    Timeout = null;
                }

                if (Linked != null)
                {
                    Linked.Dispose();
                    Linked = null;
                }
            }

            public TokenPair(TimeSpan timeout, CancellationToken token)
            {
                if (timeout == TimeSpan.Zero)
                    return;
                Timeout = new CancellationTokenSource(timeout);
                Linked = CancellationTokenSource.CreateLinkedTokenSource(Timeout.Token, token);
            }
        }

        public async Task<LogoutResponse> LogoutAsync(
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);
            return await client.LogoutAsync(pair.LinkedToken);
        }

        public async Task StopAsync(
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);
            await client.StopAsync(pair.LinkedToken);
        }

        public void TriggerStop()
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            // using var pair = new TokenPair(_timeout, token);
            client.StopAsync();
        }

        //[Login]
        public async Task<GetRecordResponse> GetRecordAsync(GetRecordRequest body,
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);

            using var pair = new TokenPair(_timeout, token);

            while (true)
            {
                var result = await client.GetRecordAsync(body, pair.LinkedToken);
                if (await CheckResult(result.GetRecordResult) == false)
                    return result;
            }
        }

        public async Task<SearchItemResponse> SearchItemAsync(SearchItemRequest body,
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);

            while (true)
            {
                var result = await client.SearchItemAsync(body, pair.LinkedToken);
                if (await CheckResult(result.SearchItemResult) == false)
                    return result;
            }
        }

        public async Task<SearchReaderResponse> SearchReaderAsync(SearchReaderRequest body,
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);

            while (true)
            {
                var result = await client.SearchReaderAsync(body, pair.LinkedToken);
                if (await CheckResult(result.SearchReaderResult) == false)
                    return result;
            }
        }

        public async Task<SearchBiblioResponse> SearchBiblioAsync(SearchBiblioRequest body,
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);

            while (true)
            {
                var result = await client.SearchBiblioAsync(body, pair.LinkedToken);
                if (await CheckResult(result.SearchBiblioResult) == false)
                    return result;
            }
        }

        public async Task<GetSearchResultResponse> GetSearchResultAsync(GetSearchResultRequest body,
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);

            while (true)
            {
                var result = await client.GetSearchResultAsync(body, pair.LinkedToken);
                if (await CheckResult(result.GetSearchResultResult) == false)
                    return result;
            }
        }

        // 管理结果集
        // parameters:
        //      strAction   share/remove 分别表示共享为全局结果集对象/删除全局结果集对象
        /// <summary>
        /// 管理结果集。
        /// 本方法实际上是由 dp2Library API GetSearchResult() 包装而来。请参考其详细介绍。
        /// strAction 为 "share" 时，strResultSetName 内为要共享出去的通道结果集名，strGlobalResultName 为要共享成的全局结果集名；
        /// strAction 为 "remove" 时，strResultSetName 参数不使用(设置为空即可)，strGlobalResultName 为要删除的颧骨结果集名
        /// </summary>
        /// <param name="strAction">动作。为 share / remove 之一</param>
        /// <param name="strResultSetName">(当前通道)结果集名</param>
        /// <param name="strGlobalResultName">全局结果集名</param>
        /// <param name="strError">返回出错信息</param>
        /// <returns>
        /// <para>-1:   出错</para>
        /// <para>0:    成功</para>
        /// </returns>
        public long ManageSearchResult(
            string strAction,
            string strResultSetName,
            string strGlobalResultName,
            out string strError)
        {
            var result = GetSearchResultAsync(
                new GetSearchResultRequest
                {
                    StrResultSetName = strResultSetName,
                    LStart = 0,
                    LCount = 0,
                    StrBrowseInfoStyle = "@" + strAction + ":" + strGlobalResultName,
                    StrLang = "zh",
                }
                ).Result;
            long lRet = result.GetSearchResultResult.Value;
            strError = result.GetSearchResultResult.ErrorInfo;
            return lRet;
            /*
            Record[] searchresults = null;
            strError = "";

        REDO:
            try
            {
                IAsyncResult soapresult = this.ws.BeginGetSearchResult(
                    strResultSetName,
                    0,
                    0,
                    "@" + strAction + ":" + strGlobalResultName,
                    "zh",
                    null,
                    null);

                WaitComplete(soapresult);

                if (this.m_ws == null)
                {
                    strError = "用户中断";
                    this.ErrorCode = localhost.ErrorCode.RequestCanceled;
                    return -1;
                }

                LibraryServerResult result = this.ws.EndGetSearchResult(
                    out searchresults,
                    soapresult);
                if (result.Value == -1 && result.ErrorCode == ErrorCode.NotLogin)
                {
                    if (DoNotLogin(ref strError) == 1)
                        goto REDO;
                    return -1;
                }
                strError = result.ErrorInfo;
                this.ErrorCode = result.ErrorCode;
                this.ClearRedoCount();
                return result.Value;
            }
            catch (Exception ex)
            {
                int nRet = ConvertWebError(ex, out strError);
                if (nRet == 0)
                    return -1;
                goto REDO;
            }
            */
        }

        public async Task<GetOperLogResponse> GetOperLogAsync(GetOperLogRequest body,
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);

            while (true)
            {
                var result = await client.GetOperLogAsync(body, pair.LinkedToken);
                if (await CheckResult(result.GetOperLogResult) == false)
                    return result;
            }
        }

        public async Task<GetOperLogsResponse> GetOperLogsAsync(GetOperLogsRequest body,
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);

            while (true)
            {
                var result = await client.GetOperLogsAsync(body, pair.LinkedToken);
                if (await CheckResult(result.GetOperLogsResult) == false)
                    return result;
            }
        }

        // return:
        //      -1  出错
        //      0   没有找到日志记录，或者附件不存在
        //      >0  附件总长度
        public long DownloadOperlogAttachment(
            // DigitalPlatform.Stop stop,
            string strFileName,
            long lIndex,
            long lHint,
            string strOutputFileName,
            out long lHintNext,
            out string strError)
        {
            strError = "";
            lHintNext = -1;

            long lRet = -1;

            using (Stream stream = File.Create(strOutputFileName))
            {
                long lAttachmentFragmentStart = 0;
                int nAttachmentFragmentLength = -1;
                byte[]? attachment_data = null;
                long lAttachmentTotalLength = 0;

                for (; ; )
                {
                    string strXml = "";
                    /*
                    lRet = this.GetOperLog(
                        stop,
                        strFileName,
                        lIndex,
                        lHint,
                "dont_return_xml", // strStyle,
                "", // strFilter,
                out strXml,
                out lHintNext,
                lAttachmentFragmentStart,
                nAttachmentFragmentLength,
                out attachment_data,
                out lAttachmentTotalLength,
                out strError);
                    */
                    var result = this.GetOperLogAsync(
                        new GetOperLogRequest
                        {
                            StrFileName = strFileName,
                            LIndex = lIndex,
                            LHint = lHint,
                            StrStyle = "dont_return_xml",
                            StrFilter = "",
                            LAttachmentFragmentStart = lAttachmentFragmentStart,
                            NAttachmentFragmentLength = nAttachmentFragmentLength,
                        }
                        ).Result;
                    lRet = result.GetOperLogResult.Value;
                    strError = result.GetOperLogResult.ErrorInfo;
                    strXml = result.StrXml;
                    lHintNext = result.LHintNext;
                    attachment_data = result.Attachment_data?.Cast<byte>().ToArray<byte>();
                    lAttachmentTotalLength = result.LAttachmentTotalLength;

                    if (lRet == -1)
                    {
                        goto DELETE_AND_RETURN;
                    }
                    // 日志记录不存在
                    if (lRet == 0)
                    {
                        lRet = 0;
                        goto DELETE_AND_RETURN;
                    }
                    // 没有附件
                    if (lAttachmentTotalLength == 0)
                    {
                        lRet = 0;
                        strError = "附件不存在";
                        goto DELETE_AND_RETURN;
                    }
                    if (attachment_data == null || attachment_data.Length == 0)
                    {
                        lRet = -1;
                        strError = "attachment_data == null || attachment_data.Length == 0";
                        goto DELETE_AND_RETURN;
                    }
                    stream.Write(attachment_data, 0, attachment_data.Length);
                    lAttachmentFragmentStart += attachment_data.Length;
                    if (lAttachmentFragmentStart >= lAttachmentTotalLength)
                        return lAttachmentTotalLength;
                }
            }
        DELETE_AND_RETURN:
            File.Delete(strOutputFileName);
            return lRet;
        }

        public async Task<GetUserResponse> GetUserAsync(GetUserRequest body,
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);

            while (true)
            {
                var result = await client.GetUserAsync(body, pair.LinkedToken);
                if (await CheckResult(result.GetUserResult) == false)
                    return result;
            }
        }

        public async Task<GetVersionResponse> GetVersionAsync(
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);

            while (true)
            {
                var result = await client.GetVersionAsync(pair.LinkedToken);
                if (await CheckResult(result.GetVersionResult) == false)
                    return result;
            }
        }

        public async Task<ListBiblioDbFromsResponse> ListBiblioDbFromsAsync(ListBiblioDbFromsRequest body,
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);

            while (true)
            {
                var result = await client.ListBiblioDbFromsAsync(body, pair.LinkedToken);
                if (await CheckResult(result.ListBiblioDbFromsResult) == false)
                    return result;
            }
        }

        public async Task<ManageDatabaseResponse> ManageDatabaseAsync(ManageDatabaseRequest body,
            CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);

            while (true)
            {
                var result = await client.ManageDatabaseAsync(body, pair.LinkedToken);
                if (await CheckResult(result.ManageDatabaseResult) == false)
                    return result;
            }
        }

        // 2023/9/14
        public async Task<GetSystemParameterResponse> GetSystemParameterAsync(GetSystemParameterRequest body,
    CancellationToken token = default)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            using var pair = new TokenPair(_timeout, token);

            while (true)
            {
                var result = await client.GetSystemParameterAsync(body, pair.LinkedToken);
                if (await CheckResult(result.GetSystemParameterResult) == false)
                    return result;
            }
        }

        #region 按需登录

        public ErrorCode ErrorCode;

        // return:
        //      false   不用重做
        //      true    需要重做
        async Task<bool> CheckResult(LibraryServerResult result)
        {
            this.ErrorCode = result.ErrorCode;
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
                    await LogoutAsync();  // 登出
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

                var result = await LoginAsync(new LoginRequest
                {
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

                this.m_nRedoCount = 0;
                if (this.AfterLogin != null)
                {
                    AfterLoginEventArgs e1 = new AfterLoginEventArgs();
                    this.AfterLogin(this, e1);
                    if (string.IsNullOrEmpty(e1.ErrorInfo) == false)
                    {
                        strError = e1.ErrorInfo;
                        return new NormalResult
                        {
                            Value = -1,
                            ErrorInfo = strError
                        };
                    }
                }

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

        #endregion
    }

}
