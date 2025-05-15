using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.FriendGroups;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Protocol.DTOs.Notifications.Groups; // 新增 using

namespace IMSystem.Server.Core.Features.FriendGroups.EventHandlers;

public class FriendGroupsReorderedEventHandler : INotificationHandler<FriendGroupsReorderedEvent>
{
    private readonly ILogger<FriendGroupsReorderedEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;

    public FriendGroupsReorderedEventHandler(
        ILogger<FriendGroupsReorderedEventHandler> logger,
        IChatNotificationService chatNotificationService)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
    }

    public async Task Handle(FriendGroupsReorderedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling FriendGroupsReorderedEvent for UserId: {UserId}. {Count} groups reordered.",
            notification.UserId, notification.ReorderedGroups.Count);

        // 使用强类型 DTO
        var payload = new FriendGroupsReorderedNotificationDto
        {
            UserId = notification.UserId,
            ReorderedGroups = notification.ReorderedGroups
                .Select(g => new FriendGroupOrderItemDto { GroupId = g.GroupId, NewOrder = g.NewOrder })
                .ToList()
        };

        string clientMethodName = "FriendGroupsReordered"; // 客户端 SignalR 方法名

        try
        {
            await _chatNotificationService.SendNotificationAsync(
                notification.UserId.ToString(),
                clientMethodName,
                payload,
                cancellationToken);
            
            _logger.LogInformation("Successfully sent FriendGroupsReordered notification to UserId: {UserId}",
                notification.UserId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error sending FriendGroupsReordered notification to UserId: {UserId}",
                notification.UserId);
        }
    }
}