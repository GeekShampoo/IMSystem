using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For User entity
using IMSystem.Server.Domain.Enums;   // For GroupMemberRole
using IMSystem.Server.Domain.Events.Groups;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class SetGroupAnnouncementCommandHandler : IRequestHandler<SetGroupAnnouncementCommand, Result>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository; // To get actor username for event
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<SetGroupAnnouncementCommandHandler> _logger;

    public SetGroupAnnouncementCommandHandler(
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<SetGroupAnnouncementCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result> Handle(SetGroupAnnouncementCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {ActorUserId} attempting to set announcement for group {GroupId}. Announcement: '{Announcement}'",
            request.ActorUserId, request.GroupId, request.Announcement ?? "CLEAR");

        var group = await _groupRepository.GetByIdWithMembersAsync(request.GroupId); // Get members for permission check
        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found for setting announcement.", request.GroupId);
            return Result.Failure("Group.NotFound", $"Group with ID {request.GroupId} not found.");
        }

        var actorMember = group.Members.FirstOrDefault(m => m.UserId == request.ActorUserId);
        if (actorMember == null || (actorMember.Role != GroupMemberRole.Owner && actorMember.Role != GroupMemberRole.Admin))
        {
            _logger.LogWarning("User {ActorUserId} is not an owner or admin of group {GroupId} and cannot set announcement. Role: {Role}",
                request.ActorUserId, request.GroupId, actorMember?.Role.ToString() ?? "Not a member");
            return Result.Failure("Group.Announcement.AccessDenied", "Only group owners or admins can set the announcement.");
        }
        
        var actorUser = await _userRepository.GetByIdAsync(request.ActorUserId);
        if (actorUser == null)
        {
             _logger.LogError("Actor user {ActorUserId} not found when setting announcement for group {GroupId}.", request.ActorUserId, request.GroupId);
            return Result.Failure("User.NotFound", "Performing user not found.");
        }

        try
        {
            string? oldAnnouncement = group.Announcement; // For event, if needed, though event only carries new
            
            group.SetAnnouncement(request.Announcement, request.ActorUserId);

            // Check if anything actually changed to avoid unnecessary save and event
            if (oldAnnouncement == group.Announcement && !(oldAnnouncement == null && request.Announcement == null) ) // Check if it was actually updated
            {
                 // If old was null and new is also null (cleared an already clear announcement), or old and new are same non-null
                if( (oldAnnouncement == null && string.IsNullOrWhiteSpace(request.Announcement)) || 
                    (oldAnnouncement != null && oldAnnouncement.Equals(request.Announcement?.Trim())) )
                {
                    _logger.LogInformation("Group {GroupId} announcement was not changed by user {ActorUserId}. No update needed.", request.GroupId, request.ActorUserId);
                    return Result.Success(); // No change
                }
            }

            // _groupRepository.Update(group); // EF Core tracks changes

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Announcement for group {GroupId} ({GroupName}) successfully set/updated by user {ActorUserId}.",
                request.GroupId, group.Name, request.ActorUserId);

            var announcementSetEvent = new GroupAnnouncementSetEvent(
                group.Id,
                group.Name,
                group.Announcement, // Current announcement from entity
                request.ActorUserId,
                actorUser.Username,
                group.AnnouncementSetAt
            );
            // 领域事件统一通过实体 AddDomainEvent 添加，禁止直接 Publish，事件将由 Outbox 机制可靠交付
            group.AddDomainEvent(announcementSetEvent);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting announcement for group {GroupId} by user {ActorUserId}.", request.GroupId, request.ActorUserId);
            return Result.Failure("Group.Announcement.UnexpectedError", "An error occurred while setting the group announcement.");
        }
    }
}