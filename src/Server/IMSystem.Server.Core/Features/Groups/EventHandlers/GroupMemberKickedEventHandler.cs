using IMSystem.Protocol.DTOs.Notifications.Groups;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.Groups;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Groups.EventHandlers;

public class GroupMemberKickedEventHandler : INotificationHandler<GroupMemberKickedEvent>
{
    private readonly ILogger<GroupMemberKickedEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IGroupMemberRepository _groupMemberRepository;

    public GroupMemberKickedEventHandler(
        ILogger<GroupMemberKickedEventHandler> logger,
        IChatNotificationService chatNotificationService,
        IGroupMemberRepository groupMemberRepository)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
        _groupMemberRepository = groupMemberRepository;
    }

    public async Task Handle(GroupMemberKickedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GroupMemberKickedEvent for GroupId: {GroupId}, KickedUser: {KickedUserId} by Actor: {ActorUserId}",
            notification.GroupId, notification.KickedUserId, notification.ActorUserId);

        // 1. Notify the kicked user - 使用规范化后的DTO
        var kickedUserNotificationPayload = new GroupMemberKickedNotificationDto
        {
            GroupId = notification.GroupId,
            GroupName = notification.GroupName,
            KickedUserId = notification.KickedUserId,
            KickedUsername = notification.KickedUsername,
            ActorUserId = notification.ActorUserId,
            ActorUsername = notification.ActorUsername
        };

        await _chatNotificationService.SendNotificationAsync(
            notification.KickedUserId.ToString(),
            "GroupMemberKicked", // Client should handle this
            kickedUserNotificationPayload,
            cancellationToken);
        
        _logger.LogInformation("Sent GroupMemberKicked to kicked user {KickedUserId} for group {GroupId}", 
            notification.KickedUserId, notification.GroupId);

        // 2. Notify remaining group members - 使用相同的DTO
        var remainingMembers = await _groupMemberRepository.GetMembersByGroupIdAsync(notification.GroupId, cancellationToken);
        
        if (remainingMembers != null && remainingMembers.Any())
        {
            foreach (var member in remainingMembers)
            {
                // Don't re-notify the kicked user if they somehow are still in this list (shouldn't happen if repo is up-to-date)
                // Also, the actor (kicker) will receive this notification as a member.
                if (member.UserId != notification.KickedUserId)
                {
                    await _chatNotificationService.SendNotificationAsync(
                        member.UserId.ToString(),
                        "GroupMemberKicked", // Client should handle this
                        kickedUserNotificationPayload, // 使用相同的DTO
                        cancellationToken);
                    _logger.LogDebug("Sent GroupMemberKicked to member {MemberId} for group {GroupId}", member.UserId, notification.GroupId);
                }
            }
            _logger.LogInformation("Sent GroupMemberKicked to {MemberCount} remaining members of group {GroupId}", 
                remainingMembers.Count(m => m.UserId != notification.KickedUserId), notification.GroupId);
        }
        else
        {
            _logger.LogWarning("No remaining members found for group {GroupId} to notify about member kick, or group is now empty.", notification.GroupId);
        }
    }
}