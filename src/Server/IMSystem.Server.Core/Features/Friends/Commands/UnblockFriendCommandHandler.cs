using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Enums; // For FriendshipStatus
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using IMSystem.Server.Domain.Events.Friends;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Friends.Commands;

public class UnblockFriendCommandHandler : IRequestHandler<UnblockFriendCommand, Result>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UnblockFriendCommandHandler> _logger;
    private readonly IMediator _mediator;

    public UnblockFriendCommandHandler(
        IFriendshipRepository friendshipRepository,
        IUnitOfWork unitOfWork,
        ILogger<UnblockFriendCommandHandler> logger,
        IMediator mediator)
    {
        _friendshipRepository = friendshipRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task<Result> Handle(UnblockFriendCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {CurrentUserId} attempting to unblock user {FriendToUnblockUserId}", request.CurrentUserId, request.FriendToUnblockUserId);

        var friendship = await _friendshipRepository.GetFriendshipBetweenUsersAsync(request.CurrentUserId, request.FriendToUnblockUserId);

        if (friendship == null)
        {
            _logger.LogInformation("No friendship record found between User {CurrentUserId} and User {FriendToUnblockUserId}. Considering unblock successful (idempotency).", request.CurrentUserId, request.FriendToUnblockUserId);
            return Result.Success(); // Idempotency: If no record, it's not blocked by current user.
        }

        // Check if the friendship is actually blocked AND by the current user.
        if (friendship.Status != FriendshipStatus.Blocked || friendship.BlockedById != request.CurrentUserId)
        {
            _logger.LogInformation("Friendship between User {CurrentUserId} and User {FriendToUnblockUserId} is not blocked by the current user. Status: {Status}, BlockedById: {BlockedById}. Considering unblock successful (idempotency).",
                request.CurrentUserId, request.FriendToUnblockUserId, friendship.Status, friendship.BlockedById);
            return Result.Success(); // Idempotency: If not blocked, or blocked by the other party, the goal of "unblocked by current user" is met.
        }
        
        // At this point, the friendship IS blocked by the current user. Proceed to unblock.
        try
        {
            // The Unblock method in Friendship entity was updated to check if unblockerId == BlockedById
            friendship.Unblock(request.CurrentUserId);
            // _friendshipRepository.Update(friendship); // EF Core tracks changes for existing entities

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User {CurrentUserId} successfully unblocked user {FriendToUnblockUserId}. FriendshipId: {FriendshipId}", request.CurrentUserId, request.FriendToUnblockUserId, friendship.Id);

            // Domain event is added within Friendship.Unblock() and handled by DbContext/Outbox.
            
            return Result.Success();
        }
        catch (InvalidOperationException ex) // Catch specific exceptions from domain entity if thrown
        {
            _logger.LogWarning(ex, "Invalid operation while User {CurrentUserId} attempting to unblock user {FriendToUnblockUserId}.", request.CurrentUserId, request.FriendToUnblockUserId);
            return Result.Failure("Friendship.Unblock.InvalidOperation", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while User {CurrentUserId} attempting to unblock user {FriendToUnblockUserId}.", request.CurrentUserId, request.FriendToUnblockUserId);
            return Result.Failure("Friendship.Unblock.UnexpectedError", "An error occurred while unblocking the friend.");
        }
    }
}