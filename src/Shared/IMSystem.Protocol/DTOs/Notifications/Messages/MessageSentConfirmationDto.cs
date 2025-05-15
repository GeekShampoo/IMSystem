using System;

namespace IMSystem.Protocol.DTOs.Notifications
{
    /// <summary>
    /// 消息发送确认通知DTO
    /// </summary>
    public class MessageSentConfirmationDto
    {
        /// <summary>
        /// 客户端生成的消息ID
        /// </summary>
        public Guid? ClientMessageId { get; set; }

        /// <summary>
        /// 发送状态，例如："Sent", "SentToGroup", "Failed"
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 服务端生成的消息ID，发送成功时返回
        /// </summary>
        public Guid? ServerMessageId { get; set; }
    }
}