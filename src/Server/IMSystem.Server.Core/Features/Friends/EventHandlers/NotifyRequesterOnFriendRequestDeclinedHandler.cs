using System.Threading;
using System.Threading.Tasks;
using IMSystem.Protocol.DTOs.Notifications.Friends;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using IMSystem.Server.Domain.Events.Friends;

namespace IMSystem.Server.Core.Features.Friends.EventHandlers;

/// <summary>
/// 处理 FriendRequestDeclinedEvent 事件，并通知原请求者。
/// </summary>
public class NotifyRequesterOnFriendRequestDeclinedHandler : INotificationHandler<FriendRequestDeclinedEvent>
{
    private readonly IChatNotificationService _chatNotificationService;
    private readonly ILogger<NotifyRequesterOnFriendRequestDeclinedHandler> _logger;

    public NotifyRequesterOnFriendRequestDeclinedHandler(
        IChatNotificationService chatNotificationService,
        ILogger<NotifyRequesterOnFriendRequestDeclinedHandler> logger)
    {
        _chatNotificationService = chatNotificationService;
        _logger = logger;
    }

    public async Task Handle(FriendRequestDeclinedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling FriendRequestDeclinedEvent for RequesterId: {RequesterId}. Decliner: {DeclinerUsername} (AddresseeId: {AddresseeId})",
            notification.RequesterId, notification.DeclinerUsername, notification.AddresseeId);

        try
        {
            // 使用规范化后的DTO
            var notificationPayload = new FriendRequestRejectedNotificationDto
            {
                FriendshipId = notification.FriendshipId,
                RejecterId = notification.AddresseeId, // The one who declined
                RejecterName = notification.DeclinerUsername, // Username of the one who declined
                // Reason 字段可选，如果事件中有拒绝原因，可以填充
                RejectedAt = DateTimeOffset.UtcNow // 事件发生的时间
            };
            
            // 向原请求者发送通知
            await _chatNotificationService.SendNotificationAsync(
                notification.RequesterId.ToString(), 
                "ReceiveFriendRequestRejected", // 修改为与DTO一致的方法名
                notificationPayload,
                cancellationToken);

            _logger.LogInformation(
                "Successfully sent FriendRequestRejected notification to RequesterId: {RequesterId} from Rejector: {RejecterName}",
                notification.RequesterId, notification.DeclinerUsername);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, 
                "Error handling FriendRequestDeclinedEvent for RequesterId: {RequesterId}, Decliner: {DeclinerUsername}",
                notification.RequesterId, notification.DeclinerUsername);
        }
    }
}