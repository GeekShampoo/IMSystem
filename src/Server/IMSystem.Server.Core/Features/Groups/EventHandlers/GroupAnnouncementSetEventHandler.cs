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

public class GroupAnnouncementSetEventHandler : INotificationHandler<GroupAnnouncementSetEvent>
{
    private readonly ILogger<GroupAnnouncementSetEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IGroupRepository _groupRepository;

    public GroupAnnouncementSetEventHandler(
        ILogger<GroupAnnouncementSetEventHandler> logger,
        IChatNotificationService chatNotificationService,
        IGroupRepository groupRepository)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
        _groupRepository = groupRepository;
    }

    public async Task Handle(GroupAnnouncementSetEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GroupAnnouncementSetEvent for GroupId: {GroupId} ({GroupName}). Actor: {ActorUserId} ({ActorUsername}). Announcement: '{Announcement}'",
            notification.GroupId, notification.GroupName,
            notification.ActorUserId, notification.ActorUsername,
            notification.Announcement ?? "CLEARED");

        var group = await _groupRepository.GetByIdWithMembersAsync(notification.GroupId);
        if (group == null || group.Members == null || !group.Members.Any())
        {
            _logger.LogWarning("Group {GroupId} not found or has no members to notify for announcement update.", notification.GroupId);
            return;
        }

        var memberIdsToNotify = group.Members.Select(m => m.UserId.ToString()).ToList();

        // 使用规范化后的DTO
        var payload = new GroupAnnouncementSetNotificationDto
        {
            GroupId = notification.GroupId,
            GroupName = notification.GroupName,
            Announcement = notification.Announcement,
            ActorUserId = notification.ActorUserId,
            ActorUsername = notification.ActorUsername,
            AnnouncementSetAt = notification.AnnouncementSetAt
        };

        string clientMethodName = "GroupAnnouncementUpdated"; 

        try
        {
            // Notify all group members about the announcement change.
            foreach (var memberId in memberIdsToNotify)
            {
                await _chatNotificationService.SendNotificationAsync(
                    memberId,
                    clientMethodName,
                    payload,
                    cancellationToken);
            }
            
            _logger.LogInformation("Successfully sent GroupAnnouncementUpdated notification to {MemberCount} members of GroupId: {GroupId}",
                memberIdsToNotify.Count, notification.GroupId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error sending GroupAnnouncementUpdated notification for GroupId: {GroupId}",
                notification.GroupId);
        }
    }
}