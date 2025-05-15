using IMSystem.Protocol.DTOs.Notifications.Groups;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.Groups;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Groups.EventHandlers;

public class GroupDeletedEventHandler : INotificationHandler<GroupDeletedEvent>
{
    private readonly ILogger<GroupDeletedEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;

    public GroupDeletedEventHandler(
        ILogger<GroupDeletedEventHandler> logger,
        IChatNotificationService chatNotificationService)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
    }

    public async Task Handle(GroupDeletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GroupDeletedEvent for GroupId: {GroupId} ({GroupName}). Actor: {ActorUserId} ({ActorUsername}). Notifying {MemberCount} former members.",
            notification.GroupId, notification.GroupName,
            notification.ActorUserId, notification.ActorUsername,
            notification.FormerMemberUserIds.Count());

        // 使用规范化后的DTO
        var payload = new GroupDeletedNotificationDto
        {
            GroupId = notification.GroupId,
            GroupName = notification.GroupName,
            Actor = new ActorDetailsDto { 
                UserId = notification.ActorUserId, 
                Username = notification.ActorUsername 
            },
            DeletedAt = DateTimeOffset.UtcNow
        };

        string clientMethodName = "GroupDeleted"; 

        try
        {
            // Notify all former group members that the group was deleted.
            if (notification.FormerMemberUserIds != null && notification.FormerMemberUserIds.Any())
            {
                foreach (var memberId in notification.FormerMemberUserIds)
                {
                    await _chatNotificationService.SendNotificationAsync(
                        memberId.ToString(),
                        clientMethodName,
                        payload,
                        cancellationToken);
                }
                _logger.LogInformation("Successfully sent GroupDeleted notification to {MemberCount} former members of GroupId: {GroupId}",
                    notification.FormerMemberUserIds.Count(), notification.GroupId);
            }
            else
            {
                _logger.LogInformation("No former members to notify for deleted GroupId: {GroupId}", notification.GroupId);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error sending GroupDeleted notification for GroupId: {GroupId}",
                notification.GroupId);
        }
    }
}