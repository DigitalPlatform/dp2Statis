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

        public TimeSpan Timeout
        {
            get
            {
                return _httpClient.Timeout;
            }
            set
            {
                _httpClient.Timeout = value;
            }
        }

        public void Close()
        {
            _ = LogoutAsync();
        }

        public string? LibraryCodeList { get; set; }
        public string? Rights { get; set; }

        public async Task<LoginResponse> LoginAsync(LoginRequest body)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            var result = await client.LoginAsync(body);
            LibraryCodeList = result.StrLibraryCode;
            Rights = result.StrRights;
            return result;
        }

        public async Task<LogoutResponse> LogoutAsync()
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);
            return await client.LogoutAsync();
        }

        public async Task<GetRecordResponse> GetRecordAsync(GetRecordRequest body)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);

            while (true)
            {
                var result = await client.GetRecordAsync(body);
                if (await CheckResult(result.GetRecordResult) == false)
                    return result;
            }
        }

        public async Task<SearchItemResponse> SearchItemAsync(SearchItemRequest body)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);

            while (true)
            {
                var result = await client.SearchItemAsync(body);
                if (await CheckResult(result.SearchItemResult) == false)
                    return result;
            }
        }

        public async Task<SearchReaderResponse> SearchReaderAsync(SearchReaderRequest body)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);

            while (true)
            {
                var result = await client.SearchReaderAsync(body);
                if (await CheckResult(result.SearchReaderResult) == false)
                    return result;
            }
        }

        public async Task<SearchBiblioResponse> SearchBiblioAsync(SearchBiblioRequest body)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);

            while (true)
            {
                var result = await client.SearchBiblioAsync(body);
                if (await CheckResult(result.SearchBiblioResult) == false)
                    return result;
            }
        }

        public async Task<GetSearchResultResponse> GetSearchResultAsync(GetSearchResultRequest body)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);

            while (true)
            {
                var result = await client.GetSearchResultAsync(body);
                if (await CheckResult(result.GetSearchResultResult) == false)
                    return result;
            }
        }

        public async Task<GetOperLogResponse> GetOperLogAsync(GetOperLogRequest body)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);

            while (true)
            {
                var result = await client.GetOperLogAsync(body);
                if (await CheckResult(result.GetOperLogResult) == false)
                    return result;
            }
        }

        public async Task<GetOperLogsResponse> GetOperLogsAsync(GetOperLogsRequest body)
        {
            dp2libraryClient client = new dp2libraryClient(Url, _httpClient);

            while (true)
            {
                var result = await client.GetOperLogsAsync(body);
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
                byte[] attachment_data = null;
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
                        new GetOperLogRequest {
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
                    attachment_data = result.Attachment_data.Cast<byte>().ToArray<byte>();
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

        #endregion
    }

}
