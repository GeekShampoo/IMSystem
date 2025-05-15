using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Notifications
{
    /// <summary>
    /// 消息撤回通知DTO
    /// </summary>
    public class MessageRecalledNotificationDto
    {
        /// <summary>
        /// 被撤回的消息ID
        /// </summary>
        public Guid MessageId { get; set; }
        
        /// <summary>
        /// 消息发送者ID
        /// </summary>
        public Guid SenderId { get; set; }
        
        /// <summary>
        /// 消息接收者ID (用户ID或群组ID)
        /// </summary>
        public Guid RecipientId { get; set; }
        
        /// <summary>
        /// 接收者类型 (用户或群组)
        /// </summary>
        public ProtocolMessageRecipientType RecipientType { get; set; }
        
        /// <summary>
        /// 执行撤回操作的用户ID
        /// </summary>
        public Guid ActorId { get; set; }
        
        /// <summary>
        /// 撤回操作的时间
        /// </summary>
        public DateTimeOffset RecalledAt { get; set; }
    }
}