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

public class LeaveGroupCommandHandler : IRequestHandler<LeaveGroupCommand, Result>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<LeaveGroupCommandHandler> _logger;

    public LeaveGroupCommandHandler(
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<LeaveGroupCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result> Handle(LeaveGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} attempting to leave group {GroupId}", request.UserId, request.GroupId);

        var group = await _groupRepository.GetByIdWithMembersAsync(request.GroupId);
        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found for user {UserId} to leave.", request.GroupId, request.UserId);
            return Result.Failure("Group.NotFound", $"Group with ID {request.GroupId} not found.");
        }

        var memberLeaving = group.Members.FirstOrDefault(m => m.UserId == request.UserId);
        if (memberLeaving == null)
        {
            _logger.LogWarning("User {UserId} is not a member of group {GroupId}.", request.UserId, request.GroupId);
            return Result.Failure("Group.Leave.NotMember", "You are not a member of this group.");
        }

        var user = await _userRepository.GetByIdAsync(request.UserId);
        if (user == null) // Should not happen if memberLeaving was found, but good practice
        {
            _logger.LogError("User entity for UserId {UserId} (member of group {GroupId}) not found.", request.UserId, request.GroupId);
            return Result.Failure("User.NotFound", "User performing leave action not found.");
        }

        // Business rule: Owner cannot leave if they are the only owner and there are other members.
        // They must transfer ownership first, or if they are the last member, leaving means disbanding.
        if (memberLeaving.Role == GroupMemberRole.Owner)
        {
            var otherOwners = group.Members.Any(m => m.UserId != request.UserId && m.Role == GroupMemberRole.Owner);
            var otherMembers = group.Members.Any(m => m.UserId != request.UserId);

            if (!otherOwners && otherMembers)
            {
                _logger.LogWarning("Owner {UserId} attempted to leave group {GroupId} with other members remaining and no other owners.", request.UserId, request.GroupId);
                return Result.Failure("Group.Leave.OwnerCannotLeave", "You are the only owner. Please transfer ownership before leaving, or remove all other members first.");
            }
        }

        try
        {
            _groupMemberRepository.Remove(memberLeaving);
            _logger.LogInformation("User {UserId} removed from GroupMembers for group {GroupId}.", request.UserId, request.GroupId);

            bool groupDisbanded = false;
            // Check if the group should be disbanded
            if (!group.Members.Any(m => m.UserId != request.UserId)) // If no members left other than the one leaving
            {
                _logger.LogInformation("Last member {UserId} left group {GroupId}. Disbanding group.", request.UserId, request.GroupId);
                _groupRepository.Remove(group); // This will also remove associated GroupMembers due to cascade delete if configured, or handle manually.
                groupDisbanded = true;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Publish events
            var memberLeftEvent = new GroupMemberLeftEvent(
                group.Id,
                group.Name,
                user.Id,
                user.Username,
                actorUsername: user.Username, // Actor is the user themselves
                wasKicked: false,             // Explicitly false for self-leave
                actorId: user.Id              // ActorId is the user themselves
            );
            // 领域事件统一通过实体 AddDomainEvent 添加，禁止直接 Publish，事件将由 Outbox 机制可靠交付
            group.AddDomainEvent(memberLeftEvent);

            if (groupDisbanded)
            {
                var formerMemberIds = new List<Guid> { request.UserId }; // Only the leaving member was left
                var groupDeletedEvent = new GroupDeletedEvent(
                    group.Id,
                    group.Name,
                    request.UserId, // Actor is the user who left
                    user.Username,  // Username of the actor
                    formerMemberIds
                );
                // 领域事件统一通过实体 AddDomainEvent 添加，禁止直接 Publish，事件将由 Outbox 机制可靠交付
                group.AddDomainEvent(groupDeletedEvent);
                return Result.Success(); // Successfully left and disbanded the group.
            }

            return Result.Success(); // Successfully left the group.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while user {UserId} attempting to leave group {GroupId}.", request.UserId, request.GroupId);
            return Result.Failure("Group.Leave.UnexpectedError", "An error occurred while leaving the group.");
        }
    }
}