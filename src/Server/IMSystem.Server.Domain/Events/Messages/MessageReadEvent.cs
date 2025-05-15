using IMSystem.Server.Domain.Common;
using IMSystem.Server.Domain.Entities; // For MessageRecipientType
using System;
using IMSystem.Server.Domain.Enums;

namespace IMSystem.Server.Domain.Events.Messages;

/// <summary>
/// 表示消息已被读取的领域事件。
/// </summary>
public class MessageReadEvent : DomainEvent
{
    /// <summary>
    /// 获取已读消息的ID。
    /// </summary>
    public Guid MessageId { get; }

    /// <summary>
    /// 获取读取该消息的用户ID。
    /// </summary>
    public Guid ReaderUserId { get; }

    /// <summary>
    /// 获取消息被读取的时间。
    /// </summary>
    public DateTimeOffset ReadAt { get; }

    /// <summary>
    /// 获取消息发送者的ID (用于通知发送者消息已读)。
    /// </summary>
    public Guid SenderUserId { get; }

    /// <summary>
    /// 获取消息接收者的ID (可以是用户ID或群组ID)。
    /// </summary>
    public Guid RecipientId { get; }

    /// <summary>
    /// 获取接收者类型 (User 或 Group)。
    /// </summary>
    public MessageRecipientType RecipientType { get; }

    /// <summary>
    /// 初始化 <see cref="MessageReadEvent"/> 类的新实例。
    /// </summary>
    /// <param name="messageId">已读消息的ID。</param>
    /// <param name="readerUserId">读取该消息的用户ID。</param>
    /// <param name="readAt">消息被读取的时间。</param>
    /// <param name="senderUserId">消息发送者的ID。</param>
    /// <param name="recipientId">消息接收者的ID。</param>
    /// <param name="recipientType">接收者类型。</param>
    public MessageReadEvent(Guid messageId, Guid readerUserId, DateTimeOffset readAt, Guid senderUserId, Guid recipientId, MessageRecipientType recipientType)
        : base(entityId: messageId, triggeredBy: readerUserId) // 消息ID作为实体ID，读者ID作为触发者ID
    {
        MessageId = messageId;
        ReaderUserId = readerUserId;
        ReadAt = readAt;
        SenderUserId = senderUserId;
        RecipientId = recipientId;
        RecipientType = recipientType;
    }
}