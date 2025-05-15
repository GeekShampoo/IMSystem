namespace IMSystem.Server.Domain.Enums
{
    /// <summary>
    /// 消息接收者类型枚举。
    /// </summary>
    public enum MessageRecipientType
    {
        /// <summary>
        /// 消息发送给单个用户。
        /// </summary>
        User,
        /// <summary>
        /// 消息发送给群组。
        /// </summary>
        Group
    }
}