using System;
using System.ComponentModel;

namespace IMSystem.Protocol.Enums
{
    /// <summary>
    /// API 错误码枚举，用于标准化错误响应
    /// </summary>
    public enum ApiErrorCode
    {
        #region 通用错误 (1000-1999)
        [Description("未知错误")]
        UnknownError = 1000,

        [Description("服务器内部错误")]
        ServerError = 1001,

        [Description("请求参数验证失败")]
        ValidationFailed = 1002,

        [Description("请求的资源不存在")]
        ResourceNotFound = 1003,

        [Description("操作被拒绝，权限不足")]
        AccessDenied = 1004,

        [Description("认证失败")]
        AuthenticationFailed = 1005,

        [Description("并发修改冲突")]
        ConcurrencyConflict = 1006,

        [Description("无效操作")]
        InvalidOperation = 1007,

        [Description("业务规则冲突")]
        BusinessRuleViolation = 1008,
 
        #endregion
 
        #region 用户相关错误 (2000-2999)
        [Description("用户名或密码错误")]
        InvalidCredentials = 2000,

        [Description("用户名已被占用")]
        UsernameAlreadyExists = 2001,

        [Description("邮箱已被注册")]
        EmailAlreadyExists = 2002,

        [Description("用户已被禁用")]
        UserDisabled = 2003,

        [Description("用户未激活")]
        UserNotActivated = 2004,

        [Description("密码复杂度不足")]
        PasswordComplexityInsufficient = 2005,

        [Description("登录尝试次数过多")]
        TooManyLoginAttempts = 2006,
        
        [Description("用户信息不完整")]
        IncompleteUserProfile = 2007,

        [Description("用户已被阻止")]
        UserAlreadyBlocked = 2008,

        [Description("不能阻止自己")]
        CannotBlockSelf = 2009,
        #endregion
 
        #region 好友相关错误 (3000-3999)
        [Description("已经是好友关系")]
        AlreadyFriends = 3000,

        [Description("好友请求已存在")]
        FriendRequestAlreadyExists = 3001,

        [Description("好友请求已过期")]
        FriendRequestExpired = 3002,

        [Description("已达到好友数量上限")]
        FriendLimitReached = 3003,

        [Description("好友分组已达上限")]
        FriendGroupLimitReached = 3004,

        [Description("好友分组名称重复")]
        DuplicateFriendGroupName = 3005,
        #endregion

        #region 群组相关错误 (4000-4999)
        [Description("群组名称已存在")]
        GroupNameAlreadyExists = 4000,

        [Description("已达到群组数量上限")]
        GroupLimitReached = 4001,

        [Description("已达到群成员上限")]
        GroupMemberLimitReached = 4002,

        [Description("无权操作此群组")]
        NoGroupPermission = 4003,

        [Description("无法移除群主")]
        CannotRemoveGroupOwner = 4004,

        [Description("群组邀请已过期")]
        GroupInvitationExpired = 4005,

        [Description("已在该群组中")]
        AlreadyInGroup = 4006,
        #endregion

        #region 消息相关错误 (5000-5999)
        [Description("消息已过期或已被撤回")]
        MessageExpiredOrRecalled = 5000,

        [Description("消息内容包含敏感词")]
        MessageContainsSensitiveWords = 5001,

        [Description("消息过长")]
        MessageTooLong = 5002,

        [Description("消息发送频率过高")]
        MessageRateLimitExceeded = 5003,

        [Description("不支持的消息类型")]
        UnsupportedMessageType = 5004,
        #endregion

        #region 文件相关错误 (6000-6999)
        [Description("文件大小超出限制")]
        FileSizeExceeded = 6000,

        [Description("不支持的文件类型")]
        UnsupportedFileType = 6001,

        [Description("文件上传失败")]
        FileUploadFailed = 6002,

        [Description("文件下载失败")]
        FileDownloadFailed = 6003,

        [Description("存储空间不足")]
        InsufficientStorage = 6004,
        #endregion

        #region 信令相关错误 (7000-7999)
        [Description("通话已结束")]
        CallAlreadyEnded = 7000,

        [Description("通话已被拒绝")]
        CallRejected = 7001,

        [Description("无法建立媒体连接")]
        MediaConnectionFailed = 7002,

        [Description("用户忙")]
        UserBusy = 7003,

        [Description("不支持的通话类型")]
        UnsupportedCallType = 7004,
        [Description("操作被禁止")]
        OperationForbidden = 7005,
        [Description("令牌无效")]
        TokenInvalid = 7006,
        [Description("令牌已过期")]
        TokenExpired = 7007,
        [Description("账户已停用")]
        AccountDeactivated = 7008,
        [Description("违反领域规则")]
        DomainRuleViolated = 7009,
        FriendshipAlreadyExists = 7010,
        #endregion
    }
}