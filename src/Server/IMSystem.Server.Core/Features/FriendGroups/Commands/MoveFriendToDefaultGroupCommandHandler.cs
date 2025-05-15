using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For UserFriendGroup entity
using IMSystem.Server.Domain.Enums;   // For FriendshipStatus
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

public class MoveFriendToDefaultGroupCommandHandler : IRequestHandler<MoveFriendToDefaultGroupCommand, Result>
{
    private readonly IUserFriendGroupRepository _userFriendGroupRepository;
    private readonly IFriendGroupRepository _friendGroupRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MoveFriendToDefaultGroupCommandHandler> _logger;

    public MoveFriendToDefaultGroupCommandHandler(
        IUserFriendGroupRepository userFriendGroupRepository,
        IFriendGroupRepository friendGroupRepository,
        IFriendshipRepository friendshipRepository,
        IUnitOfWork unitOfWork,
        ILogger<MoveFriendToDefaultGroupCommandHandler> logger)
    {
        _userFriendGroupRepository = userFriendGroupRepository;
        _friendGroupRepository = friendGroupRepository;
        _friendshipRepository = friendshipRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(MoveFriendToDefaultGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {CurrentUserId} attempting to move friend (FriendshipId: {FriendshipId}) to default group.",
            request.CurrentUserId, request.FriendshipId);

        // 1. Get the default friend group for the current user
        var defaultGroup = await _friendGroupRepository.GetDefaultByUserIdAsync(request.CurrentUserId);
        if (defaultGroup == null)
        {
            _logger.LogError("User {CurrentUserId} does not have a default friend group. This should not happen.", request.CurrentUserId);
            return Result.Failure("FriendGroup.DefaultGroupMissing", "Default friend group not found. Please contact support.");
        }

        // 2. Get the friendship to ensure it's valid and involves the current user
        var friendship = await _friendshipRepository.GetByIdAsync(request.FriendshipId);
        if (friendship == null || (friendship.RequesterId != request.CurrentUserId && friendship.AddresseeId != request.CurrentUserId))
        {
            _logger.LogWarning("FriendshipId {FriendshipId} not found or does not involve User {CurrentUserId}.", request.FriendshipId, request.CurrentUserId);
            return Result.Failure("Friendship.NotFoundOrInvalid", "Friendship not found or you are not part of this friendship.");
        }

        if (friendship.Status != FriendshipStatus.Accepted)
        {
            _logger.LogWarning("FriendshipId {FriendshipId} is not in Accepted status (Status: {Status}). Cannot move to group.", request.FriendshipId, friendship.Status);
            return Result.Failure("Friendship.NotAccepted", "Only accepted friends can be moved between groups.");
        }

        // 3. Find the existing UserFriendGroup record for this user and friendship
        // A friend should only be in one group for a given user.
        var userFriendGroup = await _userFriendGroupRepository.GetByUserIdAndFriendshipIdAsync(request.CurrentUserId, request.FriendshipId);

        if (userFriendGroup == null)
        {
            // This implies the friend was not in any group, which is unusual if they are an accepted friend.
            // Or, the logic for AddFriendToGroupCommand (when accepting a request) might need to ensure they are added to default.
            // For now, if not found, we can create a new one for the default group.
            _logger.LogInformation("No existing UserFriendGroup record for User {CurrentUserId} and FriendshipId {FriendshipId}. Creating new one for default group.",
                request.CurrentUserId, request.FriendshipId);
            
            var newUserFriendGroup = new UserFriendGroup(request.CurrentUserId, request.FriendshipId, defaultGroup.Id);
            await _userFriendGroupRepository.AddAsync(newUserFriendGroup);
        }
        else
        {
            // If the friend is already in the default group, no action needed.
            if (userFriendGroup.FriendGroupId == defaultGroup.Id)
            {
                _logger.LogInformation("Friend (FriendshipId: {FriendshipId}) is already in the default group for User {CurrentUserId}.",
                    request.FriendshipId, request.CurrentUserId);
                return Result.Success(); // No change needed
            }

            // Update the existing record to point to the default group
            userFriendGroup.MoveToGroup(defaultGroup.Id, request.CurrentUserId);
            // _userFriendGroupRepository.Update(userFriendGroup); // EF Core tracks changes
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Successfully moved friend (FriendshipId: {FriendshipId}) to default group for User {CurrentUserId}.",
                request.FriendshipId, request.CurrentUserId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving friend (FriendshipId: {FriendshipId}) to default group for User {CurrentUserId}.",
                request.FriendshipId, request.CurrentUserId);
            return Result.Failure("FriendGroup.MoveToDefaultError", "An error occurred while moving the friend to the default group.");
        }
    }
}