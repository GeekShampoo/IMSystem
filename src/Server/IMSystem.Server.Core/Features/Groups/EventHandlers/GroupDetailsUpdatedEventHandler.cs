using IMSystem.Protocol.DTOs.Notifications.Groups;
using IMSystem.Server.Core.Interfaces.Persistence; // For IGroupRepository to get members
using IMSystem.Server.Core.Interfaces.Services;   // For IChatNotificationService
using IMSystem.Server.Domain.Events.Groups;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Groups.EventHandlers;

public class GroupDetailsUpdatedEventHandler : INotificationHandler<GroupDetailsUpdatedEvent>
{
    private readonly ILogger<GroupDetailsUpdatedEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IGroupRepository _groupRepository; // To get group members

    public GroupDetailsUpdatedEventHandler(
        ILogger<GroupDetailsUpdatedEventHandler> logger,
        IChatNotificationService chatNotificationService,
        IGroupRepository groupRepository)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
        _groupRepository = groupRepository;
    }

    public async Task Handle(GroupDetailsUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GroupDetailsUpdatedEvent for GroupId: {GroupId}. Updater: {UpdaterUserId}. NewName: '{NewName}', NewDescription: '{NewDescription}', NewAvatar: '{NewAvatarUrl}'",
            notification.GroupId, notification.UpdaterUserId, notification.NewName, notification.NewDescription, notification.NewAvatarUrl);

        // Fetch the group to get its current members
        var group = await _groupRepository.GetByIdWithMembersAsync(notification.GroupId);
        if (group == null || group.Members == null || !group.Members.Any())
        {
            _logger.LogWarning("Group {GroupId} not found or has no members to notify for details update.", notification.GroupId);
            return;
        }

        var memberIds = group.Members.Select(m => m.UserId.ToString()).ToList();

        // 使用规范化后的DTO
        var payload = new GroupDetailsUpdatedNotificationDto
        {
            GroupId = notification.GroupId,
            UpdaterId = notification.UpdaterUserId,
            GroupName = notification.NewName, // 使用事件中的最新名称
            Description = notification.NewDescription, // 使用事件中的最新描述
            AvatarUrl = notification.NewAvatarUrl, // 使用事件中的最新头像URL
            UpdatedAt = DateTimeOffset.UtcNow // 设置当前时间为更新时间
        };

        string clientMethodName = "GroupDetailsUpdated"; 

        try
        {
            // Notify all group members about the update.
            // IChatNotificationService.SendMessageToGroupAsync might be more appropriate if it sends to a SignalR group.
            // Or, iterate and use SendNotificationAsync if targeting individual users.
            // For now, let's assume SendMessageToGroupAsync can take a generic payload for a specific method.
            // A more robust IChatNotificationService might have a NotifyGroupMembersAsync method.

            // Using SendNotificationAsync for each member:
            foreach (var memberId in memberIds)
            {
                await _chatNotificationService.SendNotificationAsync(
                    memberId,
                    clientMethodName,
                    payload,
                    cancellationToken);
            }
            
            _logger.LogInformation("Successfully sent GroupDetailsUpdated notification to {MemberCount} members of GroupId: {GroupId}",
                memberIds.Count, notification.GroupId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error sending GroupDetailsUpdated notification for GroupId: {GroupId}",
                notification.GroupId);
        }
    }
}