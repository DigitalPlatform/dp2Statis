using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalPlatform.LibraryClientOpenApi
{
    // API错误码
    public enum LibraryErrorCode
    {
        NoError = 0,
        SystemError = 1,    // 系统错误。指application启动时的严重错误。
        NotFound = 2,   // 没有找到
        ReaderBarcodeNotFound = 3,  // 读者证条码号不存在
        ItemBarcodeNotFound = 4,  // 册条码号不存在
        Overdue = 5,    // 还书过程发现有超期情况（已经按还书处理完毕，并且已经将超期信息记载到读者记录中，但是需要提醒读者及时履行超期违约金等手续）
        NotLogin = 6,   // 尚未登录
        DupItemBarcode = 7, // 预约中本次提交的某些册条码号被本读者先前曾预约过 TODO: 这个和 ItemBarcodeDup 是否要合并?
        InvalidParameter = 8,   // 不合法的参数
        ReturnReservation = 9,    // 还书操作成功, 因属于被预约图书, 请放入预约保留架
        BorrowReservationDenied = 10,    // 借书操作失败, 因属于被预约(到书)保留的图书, 非当前预约者不能借阅
        RenewReservationDenied = 11,    // 续借操作失败, 因属于被预约的图书
        AccessDenied = 12,  // 存取被拒绝
        // ChangePartDenied = 13,    // 部分修改被拒绝
        ItemBarcodeDup = 14,    // 册条码号重复
        Hangup = 15,    // 系统挂起
        ReaderBarcodeDup = 16,  // 读者证条码号重复(以后将改用 BarcodeDup)
        HasCirculationInfo = 17,    // 包含流通信息(不能删除)
        SourceReaderBarcodeNotFound = 18,  // 源读者证条码号不存在
        TargetReaderBarcodeNotFound = 19,  // 目标读者证条码号不存在
        FromNotFound = 20,  // 检索途径(from caption或者style)没有找到
        ItemDbNotDef = 21,  // 实体库没有定义
        IdcardNumberDup = 22,   // 身份证号检索点命中读者记录不唯一。因为无法用它借书还书。但是可以用证条码号来进行
        IdcardNumberNotFound = 23,  // 身份证号不存在
        PartialDenied = 24,  // 有部分修改被拒绝
        ChannelReleased = 25,   // 通道先前被释放过，本次操作失败
        OutofSession = 26,   // 通道达到配额上限
        InvalidReaderBarcode = 27,  // 读者证条码号不合法
        InvalidItemBarcode = 28,    // 册条码号不合法
        NeedSmsLogin = 29,  // 需要改用短信验证码方式登录
        RetryLogin = 30,    // 需要补充验证码再次登录
        TempCodeMismatch = 31,  // 验证码不匹配
        BiblioDup = 32,     // 书目记录发生重复
        Borrowing = 33,    // 图书尚未还回(盘点前需修正此问题)
        ClientVersionTooOld = 34, // 前端版本太旧
        NotBorrowed = 35,   // 册记录处于未被借出状态 2017/6/20
        NotChanged = 36,    // 没有发生修改 2019/11/10
        ServerTimeout = 37, // 服务器发生 ApplicationException 超时
        AlreadyBorrowed = 38,   // 已经被当前读者借阅 2020/3/26
        AlreadyBorrowedByOther = 39,    // 已经被其他读者借阅 2020/3/26
        SyncDenied = 40,    // 同步操作被拒绝(因为实际操作时间之后又发生过借还操作) 2020/3/27
        PasswordExpired = 41,   // 密码已经失效 2021/7/4
        BarcodeDup = 42,        // 条码号重复了 2021/8/9
        DisplayNameDup = 43,  // 显示名重复了 2021/8/9
        RefIdDup = 44,    // 参考 ID 重复了 2021/8/9
        Canceled = 45,  // 2021/11/6

        // 以下为兼容内核错误码而设立的同名错误码
        AlreadyExist = 100, // 兼容
        AlreadyExistOtherType = 101,
        ApplicationStartError = 102,
        EmptyRecord = 103,
        // None = 104, 采用了NoError
        NotFoundSubRes = 105,
        NotHasEnoughRights = 106,
        OtherError = 107,
        PartNotFound = 108,
        RequestCanceled = 109,
        RequestCanceledByEventClose = 110,
        RequestError = 111,
        RequestTimeOut = 112,
        TimestampMismatch = 113,
        Compressed = 114,   // 2017/10/7
        NotFoundObjectFile = 115, // 2019/10/7
    }

    public enum KernelErrorCode
    {
        NoError = 0,	 // 没有错误
        
        CommonError = 1, // 一般性错误   -1
        
        NotLogin = 2,	// 尚未登录 (Dir/ListTask)
        
        UserNameEmpty = 3,	// 用户名为空 (Login)
        
        UserNameOrPasswordMismatch = 4,	// 用户名或者密码错误 (Login)

        NotHasEnoughRights = 5, // 没有足够的权限 -6
        
        TimestampMismatch = 9,  //时间戳不匹配   -2
        
        NotFound = 10, //没找到记录       -4
        
        EmptyContent = 11,   //空记录  -3
        
        NotFoundDb = 12,  // 没找到数据库 -5
        
        PathError = 14, // 路径不合法  -7
        
        PartNotFound = 15, // 通过xpath未找到节点 -10
        
        ExistDbInfo = 16,  //在新建库中，发现已经存在相同的信息 -11
        
        AlreadyExist = 17,	//已经存在	-8
        
        AlreadyExistOtherType = 18,		// 存在不同类型的项 -9
        
        ApplicationStartError = 19,	//Application启动错误
        
        NotFoundSubRes = 20,    // 部分下级资源记录不存在
        
        Canceled = 21,    // 操作被放弃 2011/1/19
        
        AccessDenied = 22,  // 权限不够 2011/2/11
        
        PartialDenied = 23,  // 部分被拒绝 2012/10/9 本来是为了dp2library准备的
        
        NotFoundObjectFile = 24,  // 对象文件不存在 -100
        
        Compressed = 25,  // 返回的内容是压缩过的

        RequestError = 100,
        
        RequestTimeOut = 112,   //请求超时 2016/1/27

    };

}
