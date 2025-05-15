using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Events.Groups;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic; // For List

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class DisbandGroupCommandHandler : IRequestHandler<DisbandGroupCommand, Result>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository; // To get actor username for event
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<DisbandGroupCommandHandler> _logger;

    public DisbandGroupCommandHandler(
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<DisbandGroupCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result> Handle(DisbandGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {ActorUserId} attempting to disband group {GroupId}", request.ActorUserId, request.GroupId);

        var group = await _groupRepository.GetByIdWithMembersAsync(request.GroupId); // Get members to notify them
        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found for disbanding.", request.GroupId);
            return Result.Failure("Group.NotFound", $"Group with ID {request.GroupId} not found.");
        }

        if (group.OwnerId != request.ActorUserId)
        {
            _logger.LogWarning("User {ActorUserId} is not the owner of group {GroupId} and cannot disband it. Owner: {OwnerId}",
                request.ActorUserId, request.GroupId, group.OwnerId);
            return Result.Failure("Group.Disband.AccessDenied", "Only the group owner can disband the group.");
        }

        var actorUser = await _userRepository.GetByIdAsync(request.ActorUserId);
        if (actorUser == null)
        {
            _logger.LogError("Actor user {ActorUserId} not found when trying to disband group {GroupId}.", request.ActorUserId, request.GroupId);
            return Result.Failure("User.NotFound", "Performing user not found."); // Should not happen if actor is owner
        }

        var formerMemberUserIds = group.Members?.Select(m => m.UserId).ToList() ?? new List<Guid>();

        try
        {
            // Removing the group should cascade delete members, invitations etc. if DB is set up correctly.
            // If not, manual deletion of related entities would be needed here or in a domain service.
            _groupRepository.Remove(group);
            // Note: GroupMembers are typically removed by cascade delete.
            // If not, they would need to be removed explicitly:
            // var membersToRemove = group.Members.ToList();
            // _groupMemberRepository.RemoveRange(membersToRemove);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Group {GroupId} ({GroupName}) successfully disbanded by owner {ActorUserId}.",
                request.GroupId, group.Name, request.ActorUserId);

            var deletedEvent = new GroupDeletedEvent(
                group.Id,
                group.Name,
                request.ActorUserId,
                actorUser.Username,
                formerMemberUserIds
            );
            // 禁止直接 Publish，统一通过实体 AddDomainEvent 添加领域事件
            group.AddDomainEvent(deletedEvent);
            // await _publisher.Publish(deletedEvent, cancellationToken);
            _logger.LogInformation("Published GroupDeletedEvent for GroupId: {GroupId}", group.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disbanding group {GroupId} by user {ActorUserId}.", request.GroupId, request.ActorUserId);
            return Result.Failure("Group.Disband.UnexpectedError", "An error occurred while disbanding the group.");
        }
    }
}