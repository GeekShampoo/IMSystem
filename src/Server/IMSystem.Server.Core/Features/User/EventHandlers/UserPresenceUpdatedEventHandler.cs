using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services; 
using IMSystem.Server.Domain.Enums; 
using IMSystem.Server.Domain.Events.User;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.User.EventHandlers;

public class UserPresenceUpdatedEventHandler : INotificationHandler<UserPresenceUpdatedEvent>
{
    private readonly ILogger<UserPresenceUpdatedEventHandler> _logger;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;

    public UserPresenceUpdatedEventHandler(
        ILogger<UserPresenceUpdatedEventHandler> logger,
        IFriendshipRepository friendshipRepository,
        IChatNotificationService chatNotificationService,
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository)
    {
        _logger = logger;
        _friendshipRepository = friendshipRepository;
        _chatNotificationService = chatNotificationService;
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
    }

    public async Task Handle(UserPresenceUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling UserPresenceUpdatedEvent for User ID: {UserId}. IsOnline: {IsOnline}, CustomStatus: '{CustomStatus}'",
            notification.UserId, notification.IsOnline, notification.CustomStatus ?? "N/A");

        // 创建状态变更的通知数据
        var presencePayload = new UserPresenceNotificationPayload
        {
            UserId = notification.UserId,
            IsOnline = notification.IsOnline,
            CustomStatus = notification.CustomStatus,
            LastSeenAt = notification.LastSeenAt
        };

        // 处理通知好友
        await NotifyFriendsAsync(notification.UserId, presencePayload, cancellationToken);
        
        // 处理通知群组成员
        await NotifyGroupMembersAsync(notification.UserId, presencePayload, cancellationToken);

        _logger.LogInformation("Finished notifying related users of User ID: {UserId} about presence update.", notification.UserId);
    }

    private async Task NotifyFriendsAsync(Guid userId, UserPresenceNotificationPayload payload, CancellationToken cancellationToken)
    {
        // 获取用户的好友关系
        var friendships = await _friendshipRepository.GetUserFriendshipsAsync(userId, FriendshipStatus.Accepted);

        if (friendships == null || !friendships.Any())
        {
            _logger.LogInformation("User ID: {UserId} has no accepted friends to notify about presence update.", userId);
            return;
        }

        // 提取好友ID
        var friendIds = friendships.Select(f =>
        {
            if (f.RequesterId == userId)
            {
                return f.AddresseeId;
            }
            else if (f.AddresseeId == userId)
            {
                return f.RequesterId;
            }
            return Guid.Empty;
        }).Where(id => id != Guid.Empty).Distinct().ToList();


        _logger.LogInformation("Notifying {FriendCount} friends of User ID: {UserId} about presence update.", friendIds.Count, userId);

        // 向好友发送通知
        await _chatNotificationService.NotifyUserPresenceChangedAsync(friendIds, payload, cancellationToken);
    }

    private async Task NotifyGroupMembersAsync(Guid userId, UserPresenceNotificationPayload payload, CancellationToken cancellationToken)
    {
        // 获取用户所在的所有群组
        var userGroups = await _groupMemberRepository.GetUserGroupsAsync(userId);
        
        if (userGroups == null || !userGroups.Any())
        {
            _logger.LogInformation("User ID: {UserId} has no groups to notify members about presence update.", userId);
            return;
        }

        _logger.LogInformation("User ID: {UserId} is a member of {GroupCount} groups. Notifying other members about presence update.", 
            userId, userGroups.Count);

        // 获取每个群组中的其他成员
        var notificationRecipients = new HashSet<Guid>();
        
        foreach (var group in userGroups)
        {
            // 获取群组所有成员(排除当前用户)
            var groupMembers = await _groupMemberRepository.GetGroupMembersAsync(group.Id);
            var otherMemberIds = groupMembers
                .Where(m => m.UserId != userId)
                .Select(m => m.UserId)
                .ToList();
                
            // 添加到需要通知的用户集合
            foreach (var memberId in otherMemberIds)
            {
                notificationRecipients.Add(memberId);
            }
        }

        if (notificationRecipients.Any())
        {
            _logger.LogInformation("Notifying {MemberCount} group members of User ID: {UserId} about presence update.", 
                notificationRecipients.Count, userId);
                
            // 向群组成员发送状态变更通知
            await _chatNotificationService.NotifyUserPresenceChangedAsync(notificationRecipients.ToList(), payload, cancellationToken);
        }
    }
}