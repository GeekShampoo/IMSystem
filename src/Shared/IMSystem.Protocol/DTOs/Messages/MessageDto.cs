using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Messages
{
    /// <summary>
    /// 表示消息的数据传输对象 (通常用于服务端向客户端推送，或API响应)。
    /// </summary>
    public class MessageDto
    {
        /// <summary>
        /// 获取或设置消息的唯一标识符。
        /// </summary>
        public Guid MessageId { get; set; }

        /// <summary>
        /// 消息序列号（顺序同步与补拉用）。
        /// </summary>
        public long SequenceNumber { get; set; }

        /// <summary>
        /// 获取或设置发送者的用户ID。
        /// </summary>
        public Guid SenderId { get; set; }

        /// <summary>
        /// 获取或设置发送者的用户名，方便显示。
        /// </summary>
        public string SenderUsername { get; set; }

        /// <summary>
        /// 获取或设置发送者的头像URL（可选）。
        /// </summary>
        public string? SenderAvatarUrl { get; set; }

        /// <summary>
        /// 获取或设置接收者的ID (用户ID或群组ID)。
        /// </summary>
        public Guid RecipientId { get; set; }

        /// <summary>
        /// 获取或设置接收者的类型 ("User" 或 "Group")。
        /// </summary>
        public ProtocolMessageRecipientType RecipientType { get; set; }

        /// <summary>
        /// 获取或设置消息的内容。
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 获取或设置消息的类型 ("Text", "Image", "File" 等)。
        /// </summary>
        public ProtocolMessageType MessageType { get; set; }

        /// <summary>
        /// 获取或设置消息的发送时间。
        /// </summary>
        public DateTimeOffset SentAt { get; set; }

        /// <summary>
        /// 获取或设置消息的送达时间（可选）。
        /// </summary>
        public DateTimeOffset? DeliveredAt { get; set; }

        /// <summary>
        /// 获取或设置消息的已读时间（可选）。
        /// </summary>
        public DateTimeOffset? ReadAt { get; set; }

        /// <summary>
        /// 获取或设置回复的消息ID（可选）。
        /// </summary>
        public Guid? ReplyToMessageId { get; set; }

        /// <summary>
        /// 获取或设置群组名称（如果是群消息）。
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// 获取或设置群消息的已读数量（可选，仅对群消息有意义）。
        /// </summary>
        public int? ReadCount { get; set; }

        /// <summary>
        /// 获取或设置消息是否已与服务器同步（主要用于客户端本地存储）。
        /// 服务端下发此字段时通常为 true。客户端创建消息时应为 false 直到同步成功。
        /// </summary>
        public bool IsSynced { get; set; }
    }
}