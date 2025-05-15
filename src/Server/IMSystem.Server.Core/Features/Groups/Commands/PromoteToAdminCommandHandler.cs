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

public class PromoteToAdminCommandHandler : IRequestHandler<PromoteToAdminCommand, Result>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUserRepository _userRepository; // To get usernames for event
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<PromoteToAdminCommandHandler> _logger;

    public PromoteToAdminCommandHandler(
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<PromoteToAdminCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result> Handle(PromoteToAdminCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {ActorUserId} attempting to promote user {TargetUserId} to Admin in group {GroupId}",
            request.ActorUserId, request.TargetUserId, request.GroupId);

        var group = await _groupRepository.GetByIdWithMembersAsync(request.GroupId);
        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found for promoting member.", request.GroupId);
            return Result.Failure("Group.NotFound", $"Group with ID {request.GroupId} not found.");
        }

        var actorMember = group.Members.FirstOrDefault(m => m.UserId == request.ActorUserId);
        if (actorMember == null || actorMember.Role != GroupMemberRole.Owner)
        {
            _logger.LogWarning("User {ActorUserId} is not the owner of group {GroupId} and cannot promote members.",
                request.ActorUserId, request.GroupId);
            return Result.Failure("Group.PromoteAdmin.AccessDenied", "Only the group owner can promote members to Admin.");
        }

        var targetMember = group.Members.FirstOrDefault(m => m.UserId == request.TargetUserId);
        if (targetMember == null)
        {
            _logger.LogWarning("Target user {TargetUserId} is not a member of group {GroupId}.", request.TargetUserId, request.GroupId);
            return Result.Failure("Group.MemberNotFound", $"User {request.TargetUserId} is not a member of this group.");
        }

        if (targetMember.Role == GroupMemberRole.Admin)
        {
            _logger.LogInformation("User {TargetUserId} is already an Admin in group {GroupId}.", request.TargetUserId, request.GroupId);
            return Result.Success(); // Already an admin, no change needed.
        }
        
        if (targetMember.Role == GroupMemberRole.Owner)
        {
            _logger.LogWarning("Cannot promote owner {TargetUserId} to Admin in group {GroupId}.", request.TargetUserId, request.GroupId);
            return Result.Failure("Group.PromoteAdmin.CannotPromoteOwner", "The group owner cannot be promoted to Admin.");
        }

        var oldRole = targetMember.Role;
        var newRole = GroupMemberRole.Admin;

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

            _logger.LogInformation("User {TargetUserId} successfully promoted to Admin in group {GroupId} by user {ActorUserId}.",
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
            _logger.LogInformation("Published GroupMemberRoleUpdatedEvent for User {TargetUserId} in Group {GroupId}.", targetMember.UserId, group.Id);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while promoting user {TargetUserId} in group {GroupId}.", request.TargetUserId, request.GroupId);
            return Result.Failure("Group.PromoteAdmin.InvalidOperation", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting user {TargetUserId} to Admin in group {GroupId}.", request.TargetUserId, request.GroupId);
            return Result.Failure("Group.PromoteAdmin.UnexpectedError", "An error occurred while promoting the member to Admin.");
        }
    }
}