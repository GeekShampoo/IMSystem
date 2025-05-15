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
/// 处理 FriendRequestAcceptedEvent 事件，并通过 SignalR 实时通知原请求者。
/// 职责已明确划分：此处理器仅负责实时通知，不处理持久化操作。
/// </summary>
public class NotifyRequesterOnFriendRequestAcceptedHandler : INotificationHandler<FriendRequestAcceptedEvent>
{
    private readonly IChatNotificationService _chatNotificationService;
    private readonly ILogger<NotifyRequesterOnFriendRequestAcceptedHandler> _logger;

    public NotifyRequesterOnFriendRequestAcceptedHandler(
        IChatNotificationService chatNotificationService,
        ILogger<NotifyRequesterOnFriendRequestAcceptedHandler> logger)
    {
        _chatNotificationService = chatNotificationService;
        _logger = logger;
    }

    public async Task Handle(FriendRequestAcceptedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "实时通知处理：处理 FriendRequestAcceptedEvent，向请求者发送实时通知。RequesterId: {RequesterId}. Accepter: {AccepterUsername}",
            notification.RequesterId, notification.AccepterUsername);

        try
        {
            // 使用规范化后的DTO
            var notificationPayload = new FriendRequestAcceptedNotificationDto
            {
                FriendshipId = notification.FriendshipId,
                AcceptorId = notification.AddresseeId, // The one who accepted
                AcceptorUsername = notification.AccepterUsername,
                AcceptorNickname = notification.AccepterNickname,
                AcceptorAvatarUrl = notification.AccepterAvatarUrl,
                AcceptedAt = System.DateTimeOffset.UtcNow
            };
            
            // 向原请求者发送实时通知
            await _chatNotificationService.SendNotificationAsync(
                notification.RequesterId.ToString(), 
                "FriendRequestAccepted", // SignalR Hub 方法名
                notificationPayload,
                cancellationToken);

            _logger.LogInformation(
                "成功发送好友请求接受实时通知给 RequesterId: {RequesterId} (来自接受者: {AccepterUsername})",
                notification.RequesterId, notification.AccepterUsername);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, 
                "发送好友请求接受实时通知时出错。RequesterId: {RequesterId}, Accepter: {AccepterUsername}",
                notification.RequesterId, notification.AccepterUsername);
        }
    }
}