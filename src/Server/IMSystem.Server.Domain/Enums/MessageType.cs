namespace IMSystem.Server.Domain.Enums
{
    /// <summary>
    /// 消息类型枚举。
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// 文本消息。
        /// </summary>
        Text,
        /// <summary>
        /// 图片消息。
        /// </summary>
        Image,
        /// <summary>
        /// 文件消息。
        /// </summary>
        File,
        /// <summary>
        /// 音频消息。
        /// </summary>
        Audio,
        /// <summary>
        /// 视频消息。
        /// </summary>
        Video,
        /// <summary>
        /// 系统消息 (例如，用户加入群聊等通知)。
        /// </summary>
        System,
        /// <summary>
        /// 加密文本消息。
        /// </summary>
        EncryptedText
    }
}