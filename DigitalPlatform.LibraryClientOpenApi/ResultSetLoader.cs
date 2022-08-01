using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DigitalPlatform.LibraryClientOpenApi
{
    /// <summary>
    /// 检索命中结果的枚举器
    /// </summary>
    public class ResultSetLoader : IEnumerable
    {
        /// <summary>
        /// 提示框事件
        /// </summary>
        public event MessagePromptEventHandler Prompt = null;

        public event GettingEventHandler Getting = null;
        public event EventHandler Getted = null;

        public string ResultSetName { get; set; }

        // 最多装入的事项数。<=0 表示不限制
        public long MaxResultCount { get; set; }

        public LibraryChannel Channel { get; set; }

        // public DigitalPlatform.Stop Stop { get; set; }

        public string FormatList
        {
            get;
            set;
        }

        public string Lang { get; set; }

        // 每批获取最多多少个记录
        public long BatchSize { get; set; }

        // 从什么偏移位置开始取
        public long Start { get; set; }

        // 第一次请求后，返回结果集中的记录总数
        public long ResultCount { get; set; }

        public CancellationToken CancellationToken { get; set; }

        public ResultSetLoader(LibraryChannel channel,
            // DigitalPlatform.Stop stop,
            CancellationToken token,
            string resultsetName,
            string formatList,
            string lang = "zh")
        {
            this.Channel = channel;
            // this.Stop = stop;
            this.CancellationToken = token;
            this.ResultSetName = resultsetName;
            this.FormatList = formatList;
            this.Lang = lang;
        }

        bool IsMaxResultCountValid()
        {
            return this.MaxResultCount > 0;
        }

        public IEnumerator GetEnumerator()
        {
            string strError = "";

            this.ResultCount = 0;
            long lHitCount = -1;
            long lStart = this.Start;
            long nBasePerCount = this.BatchSize == 0 ? -1 : this.BatchSize;
            // nPerCount = 1;  // test
            for (; ; )
            {
                /*
                if (this.Stop != null && this.Stop.State != 0)
                {
                    throw new InterruptException($"用户中断");
                }
                */
                if (this.CancellationToken.IsCancellationRequested)
                    throw new InterruptException($"用户中断");

                Record[] searchresults = null;

                long nPerCount = nBasePerCount;
                if (nPerCount != -1 && IsMaxResultCountValid()
                    && nPerCount > this.MaxResultCount)
                    nPerCount = this.MaxResultCount;
                else if (nPerCount == -1 && IsMaxResultCountValid()
                    && this.MaxResultCount > this.BatchSize)
                    nPerCount = this.MaxResultCount;

                // 限制 nPerCount，不要超过余下的记录数
                if (nPerCount != -1 && lHitCount != -1
                    && nPerCount > lHitCount - lStart)
                    nPerCount = lHitCount - lStart;

                REDO:
                var e1 = new GettingEventArgs
                {
                    Start = lStart,
                    PerCount = nPerCount,
                    ResultSetName = this.ResultSetName
                };
                this.Getting?.Invoke(this, e1);
                if (e1.Cancelled == true)
                    throw new InterruptException($"用户中断");

                /*
                long lRet = this.Channel.GetSearchResult(
                    this.Stop,
                    this.ResultSetName, // "default",
                    lStart,
                    nPerCount,
                    this.FormatList,  // "id,xml,timestamp,metadata",
                    string.IsNullOrEmpty(this.Lang) ? "zh" : this.Lang,
                    out searchresults,
                    out strError);
                */
                var result = this.Channel.GetSearchResultAsync(
                    new GetSearchResultRequest { 
                    StrResultSetName = this.ResultSetName,
                    LStart = lStart,
                    LCount = nPerCount,
                    StrBrowseInfoStyle = this.FormatList,
                    StrLang = string.IsNullOrEmpty(this.Lang) ? "zh" : this.Lang,
                    }
                    ).Result;

                this.ResultCount = result.GetSearchResultResult.Value;

                var e2 = new GettedEventArgs
                {
                    Count = this.ResultCount,
                    ErrorInfo = strError,
                    ErrorCode = result.GetSearchResultResult.ErrorCode
                };
                this.Getted?.Invoke(this, e2);
                if (e2.Cancelled == true)
                    throw new InterruptException($"用户中断");

                if (this.ResultCount == -1)
                {
                    // 2022/1/12
                    if ((int)Channel.ErrorCode == (int)LibraryErrorCode.RequestCanceled)
                        throw new InterruptException($"用户中断");

                    if (this.Prompt != null)
                    {
                        MessagePromptEventArgs e = new MessagePromptEventArgs();
                        e.MessageText = "获得数据库记录时发生错误： " + strError;
                        e.Actions = "yes,no,cancel";
                        this.Prompt(this, e);
                        if (e.ResultAction == "cancel")
                            throw new ChannelException(Channel.ErrorCode, strError);
                        else if (e.ResultAction == "yes")
                            goto REDO;
                        else
                        {
                            // no 也是抛出异常。因为继续下一批代价太大
                            throw new ChannelException(Channel.ErrorCode, strError);
                        }
                    }
                    else
                        throw new ChannelException(Channel.ErrorCode, strError);
                }

                if (this.ResultCount == 0)
                    yield break;
                if (searchresults == null)
                {
                    strError = "searchresults == null";
                    throw new Exception(strError);
                }
                if (searchresults.Length == 0)
                {
                    strError = "searchresults.Length == 0";
                    throw new Exception(strError);
                }

                if (IsMaxResultCountValid() == false)
                    lHitCount = this.ResultCount;
                else
                    lHitCount = this.ResultCount == -1 ? -1 : Math.Min(MaxResultCount, this.ResultCount);

                long i = 0;
                foreach (Record record in searchresults)
                {
                    if (lStart + i >= lHitCount)
                        yield break;
                    yield return record;
                    i++;
                }

                lStart += searchresults.Length;
                if (lStart >= lHitCount)
                    yield break;
            }
        }
    }


    public delegate void GettingEventHandler(object sender,
GettingEventArgs e);

    public class GettingEventArgs : EventArgs
    {
        public long Start { get; set; }
        public long PerCount { get; set; }
        public string ResultSetName { get; set; }

        // [out]
        public bool Cancelled { get; set; }
    }

    public delegate void GettedEventHandler(object sender,
GettedEventArgs e);

    public class GettedEventArgs : EventArgs
    {
        public long Count { get; set; }
        public string ErrorInfo { get; set; }
        public ErrorCode ErrorCode { get; set; }

        // [out]
        public bool Cancelled { get; set; }
    }

    /// <summary>
    /// 消息提示事件
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    public delegate void MessagePromptEventHandler(object sender,
        MessagePromptEventArgs e);

    /// <summary>
    /// 空闲事件的参数
    /// </summary>
    public class MessagePromptEventArgs : EventArgs
    {
        public string MessageText = ""; // [in] 提示文字

        public bool IncludeOperText = false;   // [in] MessageText 提示文字中是否包含了操作说明部分？如果没有包含，则显示对话框时候要补上通用的操作说明语句

        public string[] ButtonCaptions = null;  // 按钮上希望出现的文字。如果为 null，表示由相关模块自行决定该显示什么(默认的一些文字)

        public string Actions = ""; // [in] 可选的动作。例如 "yes,no,cancel"
        public string ResultAction = "";  // [out] 返回希望采取的动作
    }

    /// <summary>
    /// dp2Library 通讯访问异常
    /// </summary>
    public class ChannelException : Exception
    {
        /// <summary>
        /// 错误码
        /// </summary>
        public ErrorCode ErrorCode;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="error"></param>
        /// <param name="strText"></param>
        public ChannelException(ErrorCode error,
            string strText)
            : base(strText)
        {
            this.ErrorCode = error;
        }
    }

}
