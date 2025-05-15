using System;
using IMSystem.Server.Domain.Common;

namespace IMSystem.Server.Domain.Events.Groups;

/// <summary>
/// Event raised when a group member\'s nickname in the group is changed.
/// </summary>
public class GroupMemberNicknameChangedEvent : DomainEvent
{
    public Guid MemberId { get; }
    public Guid GroupId { get; }
    public Guid UserId { get; }
    public string? OldNickname { get; }
    public string? NewNickname { get; }
    public Guid ModifierId { get; }

    public GroupMemberNicknameChangedEvent(
        Guid memberId,
        Guid groupId,
        Guid userId,
        string? oldNickname,
        string? newNickname,
        Guid modifierId)
    {
        MemberId = memberId;
        GroupId = groupId;
        UserId = userId;
        OldNickname = oldNickname;
        NewNickname = newNickname;
        ModifierId = modifierId;
    }
}