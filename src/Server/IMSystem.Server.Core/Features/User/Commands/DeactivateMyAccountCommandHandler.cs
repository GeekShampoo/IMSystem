using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.User.Commands;

public class DeactivateMyAccountCommandHandler : IRequestHandler<DeactivateMyAccountCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeactivateMyAccountCommandHandler> _logger;
    // Potentially IPublisher if a UserDeactivatedEvent needs to be published

    public DeactivateMyAccountCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeactivateMyAccountCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeactivateMyAccountCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} is attempting to deactivate their account.", request.UserId);

        var user = await _userRepository.GetByIdAsync(request.UserId);

        if (user == null)
        {
            // This should ideally not happen if UserId comes from an authenticated context
            _logger.LogWarning("User {UserId} not found during deactivation attempt.", request.UserId);
            return Result.Failure("User.NotFound", "User not found.");
        }

        if (user.IsDeactivated)
        {
            _logger.LogInformation("User {UserId} account is already deactivated.", request.UserId);
            return Result.Success(); // Account is already deactivated.
        }

        try
        {
            user.Deactivate(request.UserId); // User deactivates their own account

            // Consider other actions:
            // - Invalidate active sessions/tokens (requires token management/revocation list)
            // - Anonymize or soft-delete related data based on GDPR or other policies
            // - Notify other services or systems

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User {UserId} account successfully deactivated.", request.UserId);

            // TODO: Publish UserDeactivatedEvent if needed for other parts of the system to react
            // await _publisher.Publish(new UserDeactivatedEvent(user.Id), cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating account for user {UserId}.", request.UserId);
            return Result.Failure("User.Deactivation.Error", "An error occurred while deactivating the account.");
        }
    }
}