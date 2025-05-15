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

public class DemoteAdminCommandHandler : IRequestHandler<DemoteAdminCommand, Result>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUserRepository _userRepository; // To get usernames for event
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<DemoteAdminCommandHandler> _logger;

    public DemoteAdminCommandHandler(
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<DemoteAdminCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result> Handle(DemoteAdminCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {ActorUserId} attempting to demote Admin {TargetUserId} to Member in group {GroupId}",
            request.ActorUserId, request.TargetUserId, request.GroupId);

        var group = await _groupRepository.GetByIdWithMembersAsync(request.GroupId);
        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found for demoting admin.", request.GroupId);
            return Result.Failure("Group.NotFound", $"Group with ID {request.GroupId} not found.");
        }

        var actorMember = group.Members.FirstOrDefault(m => m.UserId == request.ActorUserId);
        if (actorMember == null || actorMember.Role != GroupMemberRole.Owner)
        {
            _logger.LogWarning("User {ActorUserId} is not the owner of group {GroupId} and cannot demote admins.",
                request.ActorUserId, request.GroupId);
            return Result.Failure("Group.DemoteAdmin.AccessDenied", "Only the group owner can demote Admins.");
        }

        var targetMember = group.Members.FirstOrDefault(m => m.UserId == request.TargetUserId);
        if (targetMember == null)
        {
            _logger.LogWarning("Target user {TargetUserId} is not a member of group {GroupId}.", request.TargetUserId, request.GroupId);
            return Result.Failure("Group.MemberNotFound", $"User {request.TargetUserId} is not a member of this group.");
        }

        if (targetMember.Role != GroupMemberRole.Admin)
        {
            _logger.LogInformation("User {TargetUserId} is not an Admin in group {GroupId} (Role: {Role}). Cannot demote.",
                request.TargetUserId, request.GroupId, targetMember.Role);
            return Result.Failure("Group.DemoteAdmin.NotAdmin", $"User {request.TargetUserId} is not an Admin in this group.");
        }
        
        // Cannot demote the owner
        if (targetMember.Role == GroupMemberRole.Owner)
        {
             _logger.LogWarning("Attempt to demote owner {TargetUserId} in group {GroupId} was blocked.", request.TargetUserId, request.GroupId);
            return Result.Failure("Group.DemoteAdmin.CannotDemoteOwner", "The group owner cannot be demoted.");
        }

        var oldRole = targetMember.Role;
        var newRole = GroupMemberRole.Member;

        var actorUser = await _userRepository.GetByIdAsync(request.ActorUserId);
        var targetUser = await _userRepository.GetByIdAsync(request.TargetUserId);

        if (actorUser == null || targetUser == null)
        {
             _logger.LogError("Could not find user entities for actor {ActorUserId} or target {TargetUserId}.", request.ActorUserId, request.TargetUserId);
            return Result.Failure("User.NotFound", "Internal error: User information could not be retrieved.");
        }

        try
        {
            targetMember.UpdateRole(newRole, request.ActorUserId);
            // _groupMemberRepository.Update(targetMember); // EF Core tracks changes

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User {TargetUserId} successfully demoted to Member in group {GroupId} by user {ActorUserId}.",
                request.TargetUserId, request.GroupId, request.ActorUserId);

            var roleUpdatedEvent = new GroupMemberRoleUpdatedEvent(
                group.Id,
                group.Name,
                targetMember.UserId,
                targetUser.Username,
                oldRole,
                newRole,
                request.ActorUserId,
                actorUser.Username
            );
            // 禁止直接 Publish，统一通过实体 AddDomainEvent 添加领域事件
            group.AddDomainEvent(roleUpdatedEvent);
            // await _publisher.Publish(roleUpdatedEvent, cancellationToken);
            _logger.LogInformation("Published GroupMemberRoleUpdatedEvent for User {TargetUserId} in Group {GroupId} (demotion).", targetMember.UserId, group.Id);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while demoting Admin {TargetUserId} in group {GroupId}.", request.TargetUserId, request.GroupId);
            return Result.Failure("Group.DemoteAdmin.InvalidOperation", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error demoting Admin {TargetUserId} in group {GroupId}.", request.TargetUserId, request.GroupId);
            return Result.Failure("Group.DemoteAdmin.UnexpectedError", "An error occurred while demoting the Admin.");
        }
    }
}