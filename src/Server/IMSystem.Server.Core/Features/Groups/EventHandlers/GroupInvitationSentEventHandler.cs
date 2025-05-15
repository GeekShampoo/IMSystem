using IMSystem.Protocol.DTOs.Notifications.Groups;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.Groups;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Groups.EventHandlers;

/// <summary>
/// 处理群组邀请发送事件，通知被邀请用户有新的群组邀请
/// </summary>
public class GroupInvitationSentEventHandler : INotificationHandler<GroupInvitationSentEvent>
{
    private readonly ILogger<GroupInvitationSentEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;

    public GroupInvitationSentEventHandler(
        ILogger<GroupInvitationSentEventHandler> logger,
        IChatNotificationService chatNotificationService)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
    }

    public async Task Handle(GroupInvitationSentEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理GroupInvitationSentEvent：邀请ID：{InvitationId}，群组：{GroupId}，邀请者：{InviterUserId}，被邀请用户：{InvitedUserId}",
            notification.InvitationId, notification.GroupId, notification.InviterUserId, notification.InvitedUserId);

        // 使用规范化后的DTO
        var invitationNotificationPayload = new NewGroupInvitationNotificationDto
        {
            InvitationId = notification.InvitationId,
            GroupId = notification.GroupId,
            GroupName = notification.GroupName,
            InviterId = notification.InviterUserId,
            InviterUsername = notification.InviterUsername,
            InviterNickname = null, // 原事件中没有此字段，使用null
            InviterAvatarUrl = null, // 原事件中没有此字段，使用null
            Message = notification.Message ?? $"{notification.InviterUsername}邀请您加入群组'{notification.GroupName}'。",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = notification.ExpiresAt.HasValue ? new DateTimeOffset(notification.ExpiresAt.Value) : null
        };

        // 发送通知给被邀请用户
        await _chatNotificationService.SendNotificationAsync(
            notification.InvitedUserId.ToString(), 
            "NewGroupInvitationNotification", // 客户端需要处理这个通知类型
            invitationNotificationPayload,
            cancellationToken);

        _logger.LogInformation("已成功向用户 {InvitedUserId} 发送群组 {GroupId} 的邀请通知", 
            notification.InvitedUserId, notification.GroupId);
    }
}