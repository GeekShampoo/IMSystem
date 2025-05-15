using IMSystem.Protocol.DTOs.Notifications.Groups;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.Groups;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Groups.EventHandlers;

public class GroupMemberLeftEventHandler : INotificationHandler<GroupMemberLeftEvent>
{
    private readonly ILogger<GroupMemberLeftEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IGroupRepository _groupRepository; // To get remaining members

    public GroupMemberLeftEventHandler(
        ILogger<GroupMemberLeftEventHandler> logger,
        IChatNotificationService chatNotificationService,
        IGroupRepository groupRepository)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
        _groupRepository = groupRepository;
    }

    public async Task Handle(GroupMemberLeftEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GroupMemberLeftEvent for GroupId: {GroupId} ({GroupName}), UserId: {UserId} ({Username}). WasKicked: {WasKicked}",
            notification.GroupId, notification.GroupName, notification.UserId, notification.Username, notification.WasKicked);

        // Fetch the group to get its current members to notify them.
        // The member who left/was kicked is already removed from the Members collection by the command handler
        // before this event is published (if SaveChangesAsync was called before publishing).
        // Or, if published before SaveChanges, the member might still be in the list.
        // For robustness, fetch current members.
        var group = await _groupRepository.GetByIdWithMembersAsync(notification.GroupId);
        
        // 使用规范化后的DTO
        var payload = new UserLeftGroupNotificationDto
        {
            GroupId = notification.GroupId,
            GroupName = notification.GroupName,
            UserId = notification.UserId,
            UserName = notification.Username, // Username of the user who left
            Reason = notification.WasKicked ? "Kicked" : "Left",
            ActorId = notification.ActorId, // User who performed the kick, if any
            ActorName = notification.ActorUsername, // 使用事件中的 ActorUsername
            LeftAt = DateTimeOffset.UtcNow
        };

        string clientMethodName = "UserLeftGroup";

        // Notify the user who left (if not kicked by themselves, which is not possible)
        // This allows their client to update UI, e.g., remove group from list.
        // If it was a self-leave, the client initiated it, but a server confirmation is good.
        try
        {
            await _chatNotificationService.SendNotificationAsync(
                notification.UserId.ToString(), // The user who left
                clientMethodName, // Could be a specific "YouLeftGroup" or generic "GroupUpdate"
                payload, // 使用相同的DTO对象
                cancellationToken);
            _logger.LogInformation("Sent '{ClientMethodName}' (self) notification to UserId: {UserId} for GroupId: {GroupId}",
                clientMethodName, notification.UserId, notification.GroupId);
        }
        catch (System.Exception ex)
        {
             _logger.LogError(ex, "Error sending self UserLeftGroup notification to UserId: {UserId} for GroupId: {GroupId}",
                notification.UserId, notification.GroupId);
        }


        // Notify remaining group members
        if (group != null && group.Members != null && group.Members.Any())
        {
            var remainingMemberIds = group.Members
                                          .Where(m => m.UserId != notification.UserId) // Exclude the user who left
                                          .Select(m => m.UserId.ToString())
                                          .ToList();
            
            if (remainingMemberIds.Any())
            {
                try
                {
                    // Using a generic SendNotificationToUsersAsync or similar method if available,
                    // or iterate and send one by one.
                    // For now, assuming IChatNotificationService can handle a list or we iterate.
                    // Let's assume IChatNotificationService.SendNotificationAsync is for a single user.
                    foreach (var memberId in remainingMemberIds)
                    {
                        await _chatNotificationService.SendNotificationAsync(
                            memberId,
                            clientMethodName,
                            payload, // 使用相同的DTO对象
                            cancellationToken);
                    }
                    _logger.LogInformation("Successfully sent UserLeftGroup notification to {MemberCount} remaining members of GroupId: {GroupId}",
                        remainingMemberIds.Count, notification.GroupId);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError(ex, "Error sending UserLeftGroup notification to remaining members of GroupId: {GroupId}",
                        notification.GroupId);
                }
            }
            else
            {
                _logger.LogInformation("No remaining members to notify in GroupId: {GroupId} after user {UserId} left.", notification.GroupId, notification.UserId);
            }
        }
        else
        {
            _logger.LogInformation("Group {GroupId} was likely disbanded or has no members to notify after user {UserId} left.", notification.GroupId, notification.UserId);
        }
    }
}