using System.Threading;
using System.Threading.Tasks;
using IMSystem.Protocol.DTOs.Notifications.Friends;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events;
using IMSystem.Server.Domain.Events.Friends;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMSystem.Server.Core.Features.Friends.EventHandlers;

/// <summary>
/// 处理 FriendRemovedEvent 事件，并通知被移除的好友。
/// </summary>
public class NotifyUserOnFriendRemovedHandler : INotificationHandler<FriendRemovedEvent>
{
    private readonly IChatNotificationService _chatNotificationService;
    private readonly ILogger<NotifyUserOnFriendRemovedHandler> _logger;

    public NotifyUserOnFriendRemovedHandler(
        IChatNotificationService chatNotificationService,
        ILogger<NotifyUserOnFriendRemovedHandler> logger)
    {
        _chatNotificationService = chatNotificationService;
        _logger = logger;
    }

    public async Task Handle(FriendRemovedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling FriendRemovedEvent. User {RemoverUserId} ({RemoverUsername}) removed User {RemovedFriendUserId}.",
            notification.RemoverUserId, notification.RemoverUsername, notification.RemovedFriendUserId);

        try
        {
            // 使用规范化后的DTO
            var notificationPayload = new FriendRemovedNotificationDto
            {
                FriendshipId = notification.FriendshipId,
                RemoverUserId = notification.RemoverUserId,
                RemoverUsername = notification.RemoverUsername,
                RemovedFriendUserId = notification.RemovedFriendUserId,
                RemovedFriendUsername = "Unknown" // 如果事件中没有被移除好友的用户名，标记为Unknown
                // FriendRemovedEvent 并没有包含 RemovedFriendUsername，可能需要调整事件定义或从仓储获取
            };
            
            // 向被移除的好友发送通知
            await _chatNotificationService.SendNotificationAsync(
                notification.RemovedFriendUserId.ToString(), 
                "ReceiveFriendRemoved", // SignalR Hub 方法名
                notificationPayload,
                cancellationToken);

            _logger.LogInformation(
                "Successfully sent FriendRemoved notification to User {RemovedFriendUserId} (removed by {RemoverUsername}).",
                notification.RemovedFriendUserId, notification.RemoverUsername);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, 
                "Error handling FriendRemovedEvent for RemovedFriendUserId: {RemovedFriendUserId}, Remover: {RemoverUsername}",
                notification.RemovedFriendUserId, notification.RemoverUsername);
        }
    }
}