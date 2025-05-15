using IMSystem.Protocol.DTOs.Notifications.Groups;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.Groups;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Groups.EventHandlers;

public class GroupCreatedEventHandler : INotificationHandler<GroupCreatedEvent>
{
    private readonly ILogger<GroupCreatedEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;
    // Potentially IGroupRepository to get member list if needed in future

    public GroupCreatedEventHandler(
        ILogger<GroupCreatedEventHandler> logger,
        IChatNotificationService chatNotificationService)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
    }

    public async Task Handle(GroupCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GroupCreatedEvent for GroupId: {GroupId}, Name: '{GroupName}', CreatorId: {CreatorId}",
            notification.GroupId, notification.GroupName, notification.CreatorUserId);

        // 使用规范化后的DTO
        var payload = new GroupCreatedNotificationDto
        {
            GroupId = notification.GroupId,
            GroupName = notification.GroupName,
            CreatorUserId = notification.CreatorUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Client method to handle this notification
        string clientMethodName = "GroupCreated"; 

        try
        {
            // Send notification to the creator of the group
            await _chatNotificationService.SendNotificationAsync(
                notification.CreatorUserId.ToString(),
                clientMethodName,
                payload,
                cancellationToken);
            
            _logger.LogInformation("Successfully sent GroupCreated notification to CreatorId: {CreatorId} for GroupId: {GroupId}",
                notification.CreatorUserId, notification.GroupId);
            
            // Future enhancement: If groups can be created with initial members,
            // iterate through all initial members and send them a "AddedToGroup" notification.
            // For now, only the creator is an implicit member.
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error sending GroupCreated notification to CreatorId: {CreatorId} for GroupId: {GroupId}",
                notification.CreatorUserId, notification.GroupId);
        }
    }
}