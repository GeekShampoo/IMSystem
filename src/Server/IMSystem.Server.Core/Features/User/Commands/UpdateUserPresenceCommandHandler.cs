using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Events.User; // For UserPresenceUpdatedEvent
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.User.Commands;

public class UpdateUserPresenceCommandHandler : IRequestHandler<UpdateUserPresenceCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher; // To publish domain events
    private readonly ILogger<UpdateUserPresenceCommandHandler> _logger;

    public UpdateUserPresenceCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<UpdateUserPresenceCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateUserPresenceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to update presence for User ID: {UserId}, IsOnline: {IsOnline}", request.UserId, request.IsOnline);

        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user == null)
        {
            _logger.LogWarning("User not found for User ID: {UserId} when trying to update presence.", request.UserId);
            // It's possible the user disconnected right after being deleted, or token is stale.
            // Returning success to not break hub disconnect flow, but logging a warning.
            return Result.Success(); // Success method does not take arguments for a message.
        }

        bool oldOnlineStatus = user.IsOnline;
        DateTimeOffset? oldLastSeenAt = user.LastSeenAt;

        // UpdatePresence method in User entity handles setting IsOnline and LastSeenAt
        // We pass user.CustomStatus to preserve it.
        // The modifierId is the user themselves in this context.
        user.UpdatePresence(request.IsOnline, user.CustomStatus, request.UserId);

        try
        {
            // Only save and publish event if the online status actually changed.
            if (oldOnlineStatus != user.IsOnline || (request.IsOnline == false && oldLastSeenAt != user.LastSeenAt) )
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully updated presence for User ID: {UserId}. IsOnline: {IsOnline}, LastSeenAt: {LastSeenAt}",
                    user.Id, user.IsOnline, user.LastSeenAt?.ToString("o"));

                // Publish an event that the user's presence has been updated
                var presenceEvent = new UserPresenceUpdatedEvent(
                    user.Id,
                    user.IsOnline,
                    user.CustomStatus, // Include custom status in the event
                    user.LastSeenAt
                );
                // 禁止直接 Publish，统一通过实体 AddDomainEvent 添加领域事件
                user.AddDomainEvent(presenceEvent);
                // await _publisher.Publish(presenceEvent, cancellationToken);
            }
            else
            {
                _logger.LogInformation("Presence for User ID: {UserId} was already up-to-date. IsOnline: {IsOnline}. No changes made.", request.UserId, request.IsOnline);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating presence for User ID: {UserId}", request.UserId);
            return Result.Failure("User.Presence.UpdateError", $"An error occurred while updating presence: {ex.Message}");
        }
    }
}