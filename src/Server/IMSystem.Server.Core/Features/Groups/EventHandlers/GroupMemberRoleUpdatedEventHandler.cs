using IMSystem.Protocol.DTOs.Notifications.Groups;
using IMSystem.Protocol.Enums;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.Groups;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Groups.EventHandlers;

public class GroupMemberRoleUpdatedEventHandler : INotificationHandler<GroupMemberRoleUpdatedEvent>
{
    private readonly ILogger<GroupMemberRoleUpdatedEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IGroupRepository _groupRepository;

    public GroupMemberRoleUpdatedEventHandler(
        ILogger<GroupMemberRoleUpdatedEventHandler> logger,
        IChatNotificationService chatNotificationService,
        IGroupRepository groupRepository)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
        _groupRepository = groupRepository;
    }

    public async Task Handle(GroupMemberRoleUpdatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GroupMemberRoleUpdatedEvent for GroupId: {GroupId} ({GroupName}), Member: {MemberUserId} ({MemberUsername}), OldRole: {OldRole}, NewRole: {NewRole}, Actor: {ActorUserId} ({ActorUsername})",
            notification.GroupId, notification.GroupName,
            notification.MemberUserId, notification.MemberUsername,
            notification.OldRole, notification.NewRole,
            notification.ActorUserId, notification.ActorUsername);

        var group = await _groupRepository.GetByIdWithMembersAsync(notification.GroupId);
        if (group == null || group.Members == null || !group.Members.Any())
        {
            _logger.LogWarning("Group {GroupId} not found or has no members to notify for role update.", notification.GroupId);
            return;
        }

        var memberIdsToNotify = group.Members.Select(m => m.UserId.ToString()).ToList();

        // 使用规范化后的DTO
        var payload = new GroupMemberRoleUpdatedNotificationDto
        {
            GroupId = notification.GroupId,
            GroupName = notification.GroupName,
            TargetMemberUserId = notification.MemberUserId,
            TargetMemberUsername = notification.MemberUsername,
            OldRole = (ProtocolGroupUserRole)notification.OldRole, // 这里假设枚举值兼容
            NewRole = (ProtocolGroupUserRole)notification.NewRole, // 需要确保枚举值兼容
            ActorUserId = notification.ActorUserId,
            ActorUsername = notification.ActorUsername
        };

        string clientMethodName = "GroupMemberRoleUpdated"; 

        try
        {
            // Notify all group members about the role change.
            foreach (var memberId in memberIdsToNotify)
            {
                await _chatNotificationService.SendNotificationAsync(
                    memberId,
                    clientMethodName,
                    payload,
                    cancellationToken);
            }
            
            _logger.LogInformation("Successfully sent GroupMemberRoleUpdated notification to {MemberCount} members of GroupId: {GroupId}",
                memberIdsToNotify.Count, notification.GroupId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error sending GroupMemberRoleUpdated notification for GroupId: {GroupId}",
                notification.GroupId);
        }
    }
}