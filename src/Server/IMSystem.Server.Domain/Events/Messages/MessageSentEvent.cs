using IMSystem.Server.Domain.Common; // For DomainEvent
using IMSystem.Server.Domain.Entities; // For MessageRecipientType (建议使用)
using System;
using IMSystem.Server.Domain.Enums;

namespace IMSystem.Server.Domain.Events.Messages
{
    /// <summary>
    /// 表示一条消息已被成功发送的领域事件。
    /// 此事件通常在消息持久化并准备好进行后续处理（如通知、推送等）时触发。
    /// </summary>
    public class MessageSentEvent : DomainEvent
    {
        /// <summary>
        /// 已发送消息的ID。
        /// </summary>
        public Guid MessageId { get; }

        /// <summary>
        /// 发送该消息的用户ID。
        /// </summary>
        public Guid SenderId { get; }

        /// <summary>
        /// 接收该消息的实体ID（可以是用户ID或群组ID）。
        /// </summary>
        public Guid RecipientId { get; }

        /// <summary>
        /// 接收者的类型。建议使用 <see cref="MessageRecipientType"/> 枚举。
        /// 当前为字符串，例如 "User" 或 "Group"。
        /// </summary>
        public MessageRecipientType RecipientType { get; }

        /// <summary>
        /// 消息内容的预览。
        /// 用于事件处理，避免在事件中携带完整的、可能很大的消息内容。
        /// 例如，可以是文本消息的前N个字符，或文件类型的指示。
        /// </summary>
        public string MessageContentPreview { get; }

        /// <summary>
        /// 发送者的用户名。
        /// </summary>
        public string SenderUsername { get; }

        /// <summary>
        /// 发送者的头像URL（可选）。
        /// </summary>
        public string? SenderAvatarUrl { get; }

        /// <summary>
        /// 如果是群组消息，则为群组名称（可选）。
        /// </summary>
        public string? GroupName { get; }

        // SentAt 属性已由基类 DomainEvent 的 DateOccurred 属性覆盖。

        /// <summary>
        /// 初始化 <see cref="MessageSentEvent"/> 类的新实例。
        /// </summary>
        /// <param name="messageId">已发送消息的ID。</param>
        /// <param name="senderId">发送者用户ID。</param>
        /// <param name="recipientId">接收者实体ID。</param>
        /// <param name="recipientType">接收者类型。</param>
        /// <param name="messageContentPreview">消息内容预览。</param>
        /// <param name="senderUsername">发送者用户名。</param>
        /// <param name="senderAvatarUrl">发送者头像URL。</param>
        /// <param name="groupName">群组名称（如果是群组消息）。</param>
        public MessageSentEvent(
            Guid messageId,
            Guid senderId,
            Guid recipientId,
            MessageRecipientType recipientType,
            string messageContentPreview,
            string senderUsername,
            string? senderAvatarUrl,
            string? groupName = null) // groupName is optional and defaults to null
            : base(entityId: messageId, triggeredBy: senderId) // 传递消息ID作为实体ID，发送者ID作为触发者ID
        {
            MessageId = messageId;
            SenderId = senderId;
            RecipientId = recipientId;
            RecipientType = recipientType;
            MessageContentPreview = messageContentPreview;
            SenderUsername = senderUsername;
            SenderAvatarUrl = senderAvatarUrl;

            if (recipientType == MessageRecipientType.Group)
            {
                GroupName = groupName;
            }
            else
            {
                // Ensure GroupName is null if not a group message, even if a value was passed.
                // Though the constructor parameter `groupName` defaults to null, this is an explicit safeguard.
                GroupName = null;
            }
        }
    }
}