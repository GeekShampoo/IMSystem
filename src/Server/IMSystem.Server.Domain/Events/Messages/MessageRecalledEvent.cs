using IMSystem.Server.Domain.Common;
using IMSystem.Server.Domain.Entities; // For MessageRecipientType
using System;
using IMSystem.Server.Domain.Enums;

namespace IMSystem.Server.Domain.Events.Messages;

/// <summary>
/// Event raised when a message is recalled.
/// </summary>
public class MessageRecalledEvent : DomainEvent
{
    public Guid MessageId { get; }
    public Guid SenderId { get; }
    public Guid RecipientId { get; } // User or Group ID
    public MessageRecipientType RecipientType { get; }
    public Guid ActorId { get; } // User who recalled the message (should be SenderId)
    public DateTimeOffset RecalledAt { get; }

    public MessageRecalledEvent(
        Guid messageId, 
        Guid senderId, 
        Guid recipientId, 
        MessageRecipientType recipientType, 
        Guid actorId,
        DateTimeOffset recalledAt)
        : base(entityId: messageId, triggeredBy: actorId) // 消息ID作为实体ID，召回操作者ID作为触发者ID
    {
        MessageId = messageId;
        SenderId = senderId;
        RecipientId = recipientId;
        RecipientType = recipientType;
        ActorId = actorId;
        RecalledAt = recalledAt;
    }
}