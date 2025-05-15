using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For User, Group entities
using IMSystem.Server.Domain.Enums;   // For GroupMemberRole
using IMSystem.Server.Domain.Events.Groups;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class TransferGroupOwnershipCommandHandler : IRequestHandler<TransferGroupOwnershipCommand, Result>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUserRepository _userRepository; // To get usernames for the event
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<TransferGroupOwnershipCommandHandler> _logger;

    public TransferGroupOwnershipCommandHandler(
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<TransferGroupOwnershipCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result> Handle(TransferGroupOwnershipCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {CurrentOwnerId} attempting to transfer ownership of group {GroupId} to user {NewOwnerId}",
            request.CurrentOwnerId, request.GroupId, request.NewOwnerId);

        var group = await _groupRepository.GetByIdWithMembersAsync(request.GroupId);
        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found for ownership transfer.", request.GroupId);
            return Result.Failure("Group.NotFound", $"Group with ID {request.GroupId} not found.");
        }

        if (group.OwnerId != request.CurrentOwnerId)
        {
            _logger.LogWarning("User {CurrentOwnerId} is not the owner of group {GroupId}. Current owner: {ActualOwnerId}",
                request.CurrentOwnerId, request.GroupId, group.OwnerId);
            return Result.Failure("Group.TransferOwnership.AccessDenied", "You are not the owner of this group and cannot transfer ownership.");
        }

        var currentOwnerMember = group.Members.FirstOrDefault(m => m.UserId == request.CurrentOwnerId && m.Role == GroupMemberRole.Owner);
        var newOwnerMember = group.Members.FirstOrDefault(m => m.UserId == request.NewOwnerId);

        if (newOwnerMember == null)
        {
            _logger.LogWarning("Prospective new owner {NewOwnerId} is not a member of group {GroupId}.", request.NewOwnerId, request.GroupId);
            return Result.Failure("Group.TransferOwnership.NewOwnerNotMember", $"User {request.NewOwnerId} is not a member of this group and cannot become the owner.");
        }

        if (newOwnerMember.Role == GroupMemberRole.Owner)
        {
             _logger.LogInformation("User {NewOwnerId} is already an owner of group {GroupId}.", request.NewOwnerId, request.GroupId);
            return Result.Failure("Group.TransferOwnership.AlreadyOwner", $"User {request.NewOwnerId} is already an owner of this group.");
        }
        
        var oldOwnerUser = await _userRepository.GetByIdAsync(request.CurrentOwnerId);
        var newOwnerUser = await _userRepository.GetByIdAsync(request.NewOwnerId);

        if (oldOwnerUser == null || newOwnerUser == null)
        {
            _logger.LogError("Could not find user entities for old owner {OldOwnerId} or new owner {NewOwnerId}.", request.CurrentOwnerId, request.NewOwnerId);
            return Result.Failure("User.NotFound", "Internal error: User information could not be retrieved.");
        }

        try
        {
            // 1. Update Group.OwnerId
            group.TransferOwnership(request.NewOwnerId, request.CurrentOwnerId);
            // _groupRepository.Update(group); // EF Core tracks changes

            // 2. Update roles in GroupMember table
            if (currentOwnerMember != null)
            {
                // Demote old owner to Admin (or Member, based on policy)
                currentOwnerMember.UpdateRole(GroupMemberRole.Admin, request.CurrentOwnerId); 
                _groupMemberRepository.Update(currentOwnerMember);
            }
            else
            {
                // This case should be rare if group.OwnerId matched request.CurrentOwnerId
                _logger.LogWarning("Could not find GroupMember record for current owner {CurrentOwnerId} in group {GroupId} during ownership transfer.", request.CurrentOwnerId, request.GroupId);
            }

            newOwnerMember.UpdateRole(GroupMemberRole.Owner, request.CurrentOwnerId);
            _groupMemberRepository.Update(newOwnerMember);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Ownership of group {GroupId} successfully transferred from {OldOwnerId} to {NewOwnerId} by {ActorId}.",
                request.GroupId, request.CurrentOwnerId, request.NewOwnerId, request.CurrentOwnerId);

            var transferredEvent = new GroupOwnershipTransferredEvent(
                group.Id,
                group.Name,
                request.CurrentOwnerId,
                oldOwnerUser.Username,
                request.NewOwnerId,
                newOwnerUser.Username,
                request.CurrentOwnerId // Actor is the current owner
            );
            // 禁止直接 Publish，统一通过实体 AddDomainEvent 添加领域事件
            group.AddDomainEvent(transferredEvent);
            // await _publisher.Publish(transferredEvent, cancellationToken);
            _logger.LogInformation("Published GroupOwnershipTransferredEvent for GroupId: {GroupId}", group.Id);

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during ownership transfer for group {GroupId}.", request.GroupId);
            return Result.Failure("Group.TransferOwnership.InvalidOperation", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring ownership of group {GroupId} from {CurrentOwnerId} to {NewOwnerId}.",
                request.GroupId, request.CurrentOwnerId, request.NewOwnerId);
            return Result.Failure("Group.TransferOwnership.UnexpectedError", "An error occurred while transferring group ownership.");
        }
    }
}