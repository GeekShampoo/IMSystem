using IMSystem.Server.Domain.Common;
using IMSystem.Server.Domain.Enums;
using System;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a member is added to a group.
/// </summary>
public class GroupMemberAddedEvent : DomainEvent
{
    public Guid GroupMemberId { get; }
    public Guid GroupId { get; }
    public Guid UserId { get; }
    public GroupMemberRole Role { get; }
    public string? NicknameInGroup { get; }
    public Guid AddedByUserId { get; }

    public GroupMemberAddedEvent(
        Guid groupMemberId,
        Guid groupId,
        Guid userId,
        GroupMemberRole role,
        string? nicknameInGroup,
        Guid addedByUserId)
    {
        GroupMemberId = groupMemberId;
        GroupId = groupId;
        UserId = userId;
        Role = role;
        NicknameInGroup = nicknameInGroup;
        AddedByUserId = addedByUserId;
    }
}
