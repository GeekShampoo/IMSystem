using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Notifications
{
    /// <summary>
    /// 标记消息已读确认DTO
    /// </summary>
    public class MessagesMarkedAsReadConfirmationDto
    {
        /// <summary>
        /// 聊天ID (用户ID或群组ID)
        /// </summary>
        public Guid ChatId { get; set; }
        
        /// <summary>
        /// 聊天类型
        /// </summary>
        public ProtocolChatType ChatType { get; set; }
        
        /// <summary>
        /// 状态，通常为"Read"
        /// </summary>
        public string Status { get; set; } = "Read";
        
        /// <summary>
        /// 已读到的消息ID
        /// </summary>
        public Guid? UpToMessageId { get; set; }
        
        /// <summary>
        /// 已读时间戳
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}