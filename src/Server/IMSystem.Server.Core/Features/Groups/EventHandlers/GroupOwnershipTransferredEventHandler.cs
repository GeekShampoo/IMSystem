using IMSystem.Protocol.DTOs.Notifications.Groups;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.Groups;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Groups.EventHandlers;

public class GroupOwnershipTransferredEventHandler : INotificationHandler<GroupOwnershipTransferredEvent>
{
    private readonly ILogger<GroupOwnershipTransferredEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IGroupRepository _groupRepository; // To get all group members

    public GroupOwnershipTransferredEventHandler(
        ILogger<GroupOwnershipTransferredEventHandler> logger,
        IChatNotificationService chatNotificationService,
        IGroupRepository groupRepository)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
        _groupRepository = groupRepository;
    }

    public async Task Handle(GroupOwnershipTransferredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GroupOwnershipTransferredEvent for GroupId: {GroupId} ({GroupName}). OldOwner: {OldOwnerId} ({OldOwnerUsername}), NewOwner: {NewOwnerId} ({NewOwnerUsername}), Actor: {ActorId}",
            notification.GroupId, notification.GroupName, 
            notification.OldOwnerUserId, notification.OldOwnerUsername,
            notification.NewOwnerUserId, notification.NewOwnerUsername,
            notification.ActorUserId);

        var group = await _groupRepository.GetByIdWithMembersAsync(notification.GroupId);
        if (group == null || group.Members == null || !group.Members.Any())
        {
            _logger.LogWarning("Group {GroupId} not found or has no members to notify for ownership transfer.", notification.GroupId);
            return;
        }

        var memberIds = group.Members.Select(m => m.UserId.ToString()).ToList();

        // 使用规范化后的DTO
        var payload = new GroupOwnershipTransferredNotificationDto
        {
            GroupId = notification.GroupId,
            GroupName = notification.GroupName,
            OldOwner = new UserIdentifierDto 
            { 
                UserId = notification.OldOwnerUserId, 
                Username = notification.OldOwnerUsername 
            },
            NewOwner = new UserIdentifierDto 
            { 
                UserId = notification.NewOwnerUserId, 
                Username = notification.NewOwnerUsername 
            },
            ActorUserId = notification.ActorUserId,
            ActorUsername = "System" // 默认值，因为原事件中不包含此字段
        };

        string clientMethodName = "GroupOwnershipTransferred"; 

        try
        {
            // Notify all group members about the ownership change.
            foreach (var memberId in memberIds)
            {
                await _chatNotificationService.SendNotificationAsync(
                    memberId,
                    clientMethodName,
                    payload,
                    cancellationToken);
            }
            
            _logger.LogInformation("Successfully sent GroupOwnershipTransferred notification to {MemberCount} members of GroupId: {GroupId}",
                memberIds.Count, notification.GroupId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error sending GroupOwnershipTransferred notification for GroupId: {GroupId}",
                notification.GroupId);
        }
    }
}