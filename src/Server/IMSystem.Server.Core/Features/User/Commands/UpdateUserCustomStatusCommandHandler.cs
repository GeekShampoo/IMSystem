using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Events.User; // For UserPresenceUpdatedEvent
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.User.Commands;

public class UpdateUserCustomStatusCommandHandler : IRequestHandler<UpdateUserCustomStatusCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher; // To publish domain events
    private readonly ILogger<UpdateUserCustomStatusCommandHandler> _logger;

    public UpdateUserCustomStatusCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<UpdateUserCustomStatusCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateUserCustomStatusCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to update custom status for User ID: {UserId}", request.UserId);

        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user == null)
        {
            _logger.LogWarning("User not found for User ID: {UserId} when trying to update custom status.", request.UserId);
            return Result.Failure("User.NotFound", $"User not found with ID {request.UserId}.");
        }

        try
        {
            // Use the existing UpdatePresence method, preserving the current online status.
            // The modifierId is the current user performing the action.
            string? oldCustomStatus = user.CustomStatus;
            user.UpdatePresence(user.IsOnline, request.CustomStatus, request.UserId);
            
            // _userRepository.Update(user); // Not strictly necessary if using EF Core change tracking and user is tracked
            // and UpdatePresence modifies the entity state.
            
            if (oldCustomStatus != user.CustomStatus) // Only save and publish if custom status actually changed
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully updated custom status for User ID: {UserId} to '{CustomStatus}'", request.UserId, user.CustomStatus ?? "cleared");

                // Publish an event that the user's presence (specifically custom status part) has been updated
                var presenceEvent = new UserPresenceUpdatedEvent(
                    user.Id,
                    user.IsOnline, // This should be the current online status, which we preserved
                    user.CustomStatus,
                    user.LastSeenAt // This would not have changed if only custom status was updated
                );
                // 禁止直接 Publish，统一通过实体 AddDomainEvent 添加领域事件
                user.AddDomainEvent(presenceEvent);
                // await _publisher.Publish(presenceEvent, cancellationToken);
            }
            else
            {
                _logger.LogInformation("Custom status for User ID: {UserId} was already '{CustomStatus}'. No changes made.", request.UserId, user.CustomStatus ?? "cleared");
            }
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating custom status for User ID: {UserId}", request.UserId);
            return Result.Failure("User.CustomStatus.UpdateError", $"An error occurred while updating the custom status: {ex.Message}");
        }
    }
}