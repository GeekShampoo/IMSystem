using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.FriendGroups;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Protocol.DTOs.Notifications.Groups; // 新增 using

namespace IMSystem.Server.Core.Features.FriendGroups.EventHandlers;

public class FriendGroupUpdatedEventHandler : INotificationHandler<FriendGroupUpdatedEvent>
{
    private readonly ILogger<FriendGroupUpdatedEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;

    public FriendGroupUpdatedEventHandler(
        ILogger<FriendGroupUpdatedEventHandler> logger,
        IChatNotificationService chatNotificationService)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
    }

    public async Task Handle(FriendGroupUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling FriendGroupUpdatedEvent for GroupId: {GroupId}, UserId: {UserId}. OldName: '{OldName}', NewName: '{NewName}', OldOrder: {OldOrder}, NewOrder: {NewOrder}",
            notification.GroupId, notification.UserId, notification.OldName, notification.NewName, notification.OldOrder, notification.NewOrder);

        // 使用强类型 DTO
        var payload = new FriendGroupUpdatedNotificationDto
        {
            GroupId = notification.GroupId,
            NewName = notification.NewName,
            NewOrder = notification.NewOrder,
            IsDefault = notification.IsDefault
        };

        string clientMethodName = "FriendGroupUpdated"; // 客户端 SignalR 方法名

        try
        {
            await _chatNotificationService.SendNotificationAsync(
                notification.UserId.ToString(),
                clientMethodName,
                payload,
                cancellationToken);
            
            _logger.LogInformation("Successfully sent FriendGroupUpdated notification to UserId: {UserId} for GroupId: {GroupId}",
                notification.UserId, notification.GroupId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error sending FriendGroupUpdated notification to UserId: {UserId} for GroupId: {GroupId}",
                notification.UserId, notification.GroupId);
        }
    }
}