using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.Friends;

/// <summary>
/// Event raised when a user updates the remark for a friend.
/// </summary>
public class FriendRemarkUpdatedEvent : DomainEvent
{
    public Guid OperatorUserId { get; }
    public Guid FriendUserId { get; }
    public Guid FriendshipId { get; }
    public string? NewRemark { get; }
    public bool IsRequesterToAddressee { get; }

    public FriendRemarkUpdatedEvent(Guid operatorUserId, Guid friendUserId, Guid friendshipId, string? newRemark, bool isRequesterToAddressee)
    {
        OperatorUserId = operatorUserId;
        FriendUserId = friendUserId;
        FriendshipId = friendshipId;
        NewRemark = newRemark;
        IsRequesterToAddressee = isRequesterToAddressee;
    }
}
