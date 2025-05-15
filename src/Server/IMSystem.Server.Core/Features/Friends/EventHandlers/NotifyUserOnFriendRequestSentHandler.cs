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
/// 处理 FriendRequestSentEvent 事件，并通知接收者。
/// </summary>
public class NotifyUserOnFriendRequestSentHandler : INotificationHandler<FriendRequestSentEvent>
{
    private readonly IChatNotificationService _chatNotificationService;
    private readonly ILogger<NotifyUserOnFriendRequestSentHandler> _logger;

    public NotifyUserOnFriendRequestSentHandler(
        IChatNotificationService chatNotificationService,
        ILogger<NotifyUserOnFriendRequestSentHandler> logger)
    {
        _chatNotificationService = chatNotificationService;
        _logger = logger;
    }

    public async Task Handle(FriendRequestSentEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling FriendRequestSentEvent for AddresseeId: {AddresseeId} from RequesterId: {RequesterId}",
            notification.AddresseeId, notification.RequesterId);

        try
        {
            // 使用规范化后的DTO
            var notificationPayload = new NewFriendRequestNotificationDto
            {
                FriendshipId = notification.FriendshipId,
                RequesterId = notification.RequesterId,
                RequesterUsername = notification.RequesterUsername,
                RequesterNickname = notification.RequesterNickname,
                RequesterAvatarUrl = notification.RequesterAvatarUrl,
                RequesterRemark = null, // 原事件中没有此字段，使用null
                RequestedAt = System.DateTimeOffset.UtcNow,
                ExpiresAt = null // 原事件中没有此字段，使用null
            };
            
            // 向接收者发送通知
            await _chatNotificationService.SendNotificationAsync(
                userId: notification.AddresseeId.ToString(),
                methodName: "NewFriendRequest", // SignalR Hub 方法名
                payload: notificationPayload,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Successfully sent friend request notification to AddresseeId: {AddresseeId} from RequesterId: {RequesterId}",
                notification.AddresseeId, notification.RequesterId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error handling FriendRequestSentEvent for AddresseeId: {AddresseeId} from RequesterId: {RequesterId}",
                notification.AddresseeId, notification.RequesterId);
            // 根据需要决定是否重试或进行其他错误处理
        }
    }
}