using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Protocol.Common; // Added for Result

namespace IMSystem.Server.Core.Features.Friends.Commands
{
    /// <summary>
    /// Handler for the <see cref="SetFriendRemarkCommand"/>.
    /// </summary>
    public class SetFriendRemarkCommandHandler : IRequestHandler<SetFriendRemarkCommand, Result> // Changed to return Result
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SetFriendRemarkCommandHandler> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetFriendRemarkCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work.</param>
        /// <param name="logger">The logger.</param>
        public SetFriendRemarkCommandHandler(IUnitOfWork unitOfWork, ILogger<SetFriendRemarkCommandHandler> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles the command to set a friend remark.
        /// </summary>
        /// <param name="request">The command request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation, with a Result indicating success or failure.</returns>
        public async Task<Result> Handle(SetFriendRemarkCommand request, CancellationToken cancellationToken) // Changed return type
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // It's generally better to catch specific exceptions and return Result.Failure here,
            // but for now, we'll let them propagate to align with existing controller-level try-catch.
            // A more robust implementation would wrap the logic in a try-catch.
            // try
            // {
                var friendships = await _unitOfWork.Friendships.FindAsync(
                    f => (f.RequesterId == request.CurrentUserId && f.AddresseeId == request.FriendUserId) ||
                         (f.RequesterId == request.FriendUserId && f.AddresseeId == request.CurrentUserId),
                    cancellationToken: cancellationToken);
                var friendship = friendships.FirstOrDefault();

                if (friendship == null)
                {
                    _logger.LogWarning("Friendship not found between user {CurrentUserId} and {FriendUserId}", request.CurrentUserId, request.FriendUserId);
                    // Consider returning Result.Failure here if controller doesn't specifically handle EntityNotFoundException for this path
                    throw new EntityNotFoundException(nameof(Friendship), $"with User1Id {request.CurrentUserId} and User2Id {request.FriendUserId}");
                }

                // Ensure the friendship is in an accepted state to set a remark
                if (friendship.Status != Domain.Enums.FriendshipStatus.Accepted)
                {
                    _logger.LogWarning("Cannot set remark for a friendship that is not accepted. Current status: {Status}", friendship.Status);
                    // Consider returning Result.Failure here
                    throw new InvalidOperationException($"Cannot set remark. Friendship status is {friendship.Status}.");
                }

                friendship.UpdateRemark(request.CurrentUserId, request.Remark);

                await _unitOfWork.CompleteAsync(cancellationToken);

                _logger.LogInformation("Remark updated for friend {FriendUserId} by user {CurrentUserId}", request.FriendUserId, request.CurrentUserId);
                
                return Result.Success(); // Return success
            // }
            // catch (EntityNotFoundException ex)
            // {
            //     _logger.LogError(ex, "Entity not found while setting friend remark.");
            //     return Result.Failure(ex.Message); // Or a more user-friendly error code/message
            // }
            // catch (InvalidOperationException ex)
            // {
            //     _logger.LogError(ex, "Invalid operation while setting friend remark.");
            //     return Result.Failure(ex.Message);
            // }
            // catch (Exception ex)
            // {
            //     _logger.LogError(ex, "An unexpected error occurred while setting friend remark.");
            //     return Result.Failure("An unexpected error occurred.");
            // }
        }
    }
}