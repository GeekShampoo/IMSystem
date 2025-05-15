namespace IMSystem.Server.Domain.Enums
{
    /// <summary>
    /// 定义好友关系的状态。
    /// </summary>
    public enum FriendshipStatus
    {
        /// <summary>
        /// 已发送好友请求，等待对方批准。
        /// </summary>
        Pending,
        /// <summary>
        /// 好友关系已接受，双方成为好友。
        /// </summary>
        Accepted,
        /// <summary>
        /// 好友请求已被对方拒绝。
        /// </summary>
        Declined,
        /// <summary>
        /// 一方用户阻止了另一方用户（单向或双向，取决于业务逻辑）。
        /// </summary>
        Blocked
    }
}