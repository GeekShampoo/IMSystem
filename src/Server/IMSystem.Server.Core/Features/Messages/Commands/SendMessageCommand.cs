using MediatR;
using System;
using IMSystem.Protocol.Common; // 使用新创建的 Result 类

namespace IMSystem.Server.Core.Features.Messages.Commands
{
    /// <summary>
    /// 表示发送消息的命令。
    /// </summary>
    public class SendMessageCommand : IRequest<Result<Guid>>
    {
        /// <summary>
        /// 发送者ID。
        /// </summary>
        public Guid SenderId { get; set; }

        /// <summary>
        /// 接收者ID (用户ID)。
        /// </summary>
        public Guid RecipientId { get; set; }

        /// <summary>
        /// 消息内容。
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 消息类型 (例如："Text", "Image")。
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// 客户端生成的消息ID（可选），用于去重或跟踪。
        /// </summary>
        public Guid? ClientMessageId { get; set; }

        /// <summary>
        /// 回复的消息ID（可选）。
        /// </summary>
        public Guid? ReplyToMessageId { get; set; }

        /// <summary>
        /// 接收者类型 ("User" 或 "Group")。
        /// </summary>
        public string RecipientType { get; set; }
    }
}