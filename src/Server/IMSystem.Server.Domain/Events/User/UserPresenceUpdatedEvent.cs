using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.User;

/// <summary>
/// Event raised when a user's presence (online status or custom status) is updated.
/// </summary>
public class UserPresenceUpdatedEvent : DomainEvent
{
    /// <summary>
    /// The ID of the user whose presence was updated.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// The new online status of the user.
    /// </summary>
    public bool IsOnline { get; }

    /// <summary>
    /// The new custom status of the user (can be null).
    /// </summary>
    public string? CustomStatus { get; }

    /// <summary>
    /// The user's last seen timestamp, relevant if IsOnline is false.
    /// </summary>
    public DateTimeOffset? LastSeenAt { get; }


    public UserPresenceUpdatedEvent(Guid userId, bool isOnline, string? customStatus, DateTimeOffset? lastSeenAt)
        : base(entityId: userId, triggeredBy: userId) // 用户是自己更新状态的实体和触发者
    {
        UserId = userId;
        IsOnline = isOnline;
        CustomStatus = customStatus;
        LastSeenAt = lastSeenAt;
    }
}