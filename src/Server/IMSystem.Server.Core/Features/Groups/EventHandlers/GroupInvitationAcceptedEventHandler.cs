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

public class GroupInvitationAcceptedEventHandler : INotificationHandler<GroupInvitationAcceptedEvent>
{
    private readonly ILogger<GroupInvitationAcceptedEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly ISignalRConnectionService _signalRConnectionService;

    public GroupInvitationAcceptedEventHandler(
        ILogger<GroupInvitationAcceptedEventHandler> logger,
        IChatNotificationService chatNotificationService,
        IGroupMemberRepository groupMemberRepository,
        ISignalRConnectionService signalRConnectionService)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
        _groupMemberRepository = groupMemberRepository;
        _signalRConnectionService = signalRConnectionService;
    }

    public async Task Handle(GroupInvitationAcceptedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "处理GroupInvitationAcceptedEvent：群组ID：{GroupId}，用户：{UserId}（由{InviterUserId}邀请）",
            notification.GroupId, notification.UserId, notification.InviterUserId);

        // 1. 通知邀请者 - 使用规范化后的UserJoinedGroupNotificationDto
        var inviterNotificationPayload = new UserJoinedGroupNotificationDto
        {
            GroupId = notification.GroupId,
            GroupName = notification.GroupName,
            UserId = notification.UserId,
            Username = notification.Username,
            InviterId = notification.InviterUserId,
            InviterUsername = "Inviter", // 没有InviterUsername字段，使用默认值
            JoinedAt = DateTimeOffset.UtcNow
        };

        // Ensure InviterUserId is valid before sending notification
        if (notification.InviterUserId != Guid.Empty)
        {
            await _chatNotificationService.SendNotificationAsync(
                notification.InviterUserId.ToString(),
                "GroupInvitationAccepted", // Client should handle this
                inviterNotificationPayload,
                cancellationToken);
            _logger.LogInformation("Sent GroupInvitationAccepted to inviter {InviterUserId} for group {GroupId}",
                notification.InviterUserId, notification.GroupId);
        }

        // 2. 通知接受邀请的用户（新成员）- 使用同样的DTO，但可能包含不同的内容
        var newUserNotificationPayload = new UserJoinedGroupNotificationDto
        {
            GroupId = notification.GroupId,
            GroupName = notification.GroupName,
            UserId = notification.UserId,
            Username = notification.Username,
            InviterId = notification.InviterUserId,
            InviterUsername = "Inviter", // 没有InviterUsername字段，使用默认值
            JoinedAt = DateTimeOffset.UtcNow
        };

        await _chatNotificationService.SendNotificationAsync(
            notification.UserId.ToString(),
            "UserJoinedGroup", // Client should handle this
            newUserNotificationPayload,
            cancellationToken);
        _logger.LogInformation("Sent UserJoinedGroup to new member {UserId} for group {GroupId}",
            notification.UserId, notification.GroupId);

        // 3. 通知群组中的其他现有成员 - 使用同样的DTO
        var existingMembers = await _groupMemberRepository.GetMembersByGroupIdAsync(notification.GroupId, cancellationToken);

        if (existingMembers != null && existingMembers.Any())
        {
            var memberNotificationPayload = new UserJoinedGroupNotificationDto
            {
                GroupId = notification.GroupId,
                GroupName = notification.GroupName,
                UserId = notification.UserId,
                Username = notification.Username,
                InviterId = notification.InviterUserId,
                InviterUsername = "Inviter", // 没有InviterUsername字段，使用默认值
                JoinedAt = DateTimeOffset.UtcNow
            };

            foreach (var member in existingMembers)
            {
                // Don't notify the new member again, or the inviter if they are also a regular member (they got a specific notification)
                if (member.UserId != notification.UserId && member.UserId != notification.InviterUserId)
                {
                    await _chatNotificationService.SendNotificationAsync(
                        member.UserId.ToString(),
                        "GroupMemberJoined", // Client should handle this
                        memberNotificationPayload,
                        cancellationToken);
                    _logger.LogDebug("Sent GroupMemberJoined to existing member {MemberId} for group {GroupId}", member.UserId, notification.GroupId);
                }
            }
            _logger.LogInformation("Sent GroupMemberJoined to {MemberCount} existing members of group {GroupId}",
                existingMembers.Count(m => m.UserId != notification.UserId && m.UserId != notification.InviterUserId), notification.GroupId);
        }
        else
        {
            _logger.LogWarning("No other existing members found for group {GroupId} to notify about new member, or group was empty before this user.", notification.GroupId);
        }

        // 4. 将新成员添加到SignalR群组
        try
        {
            // 使用抽象服务将用户添加到SignalR群组
            int connectionCount = await _signalRConnectionService.AddUserToSignalRGroupAsync(
                notification.UserId, 
                notification.GroupId, 
                cancellationToken);
            
            if (connectionCount > 0)
            {
                _logger.LogInformation(
                    "已将用户 {UserId} 的 {ConnectionCount} 个连接添加到SignalR群组 {GroupId}", 
                    notification.UserId, connectionCount, notification.GroupId);
            }
            else
            {
                _logger.LogInformation(
                    "用户 {UserId} 目前没有活跃的SignalR连接，无法添加到群组 {GroupId}。用户下次连接时将自动加入群组。", 
                    notification.UserId, notification.GroupId);
            }
        }
        catch (Exception ex)
        {
            // 记录错误但不要中断流程，因为这是次要操作，主要的群组成员关系已经在数据库中建立
            _logger.LogError(
                ex, 
                "将用户 {UserId} 添加到SignalR群组 {GroupId} 时发生错误", 
                notification.UserId, notification.GroupId);
        }
    }
}