using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.User.Commands;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to verify email with token: {Token}", request.Token);

        // We need a way to find the user by their verification token.
        // This might require a new method in IUserRepository.
        var user = await _userRepository.FindByEmailVerificationTokenAsync(request.Token);

        if (user == null)
        {
            _logger.LogWarning("Email verification failed: No user found with token {Token} or token is invalid/expired.", request.Token);
            return Result.Failure("User.VerifyEmail.TokenNotFound", "Invalid or expired email verification token.");
        }

        if (user.IsEmailVerified)
        {
            _logger.LogInformation("Email for user {UserId} is already verified.", user.Id);
            return Result.Success(); // Email is already verified, treat as success.
        }

        bool verificationSuccess = user.VerifyEmail(request.Token);

        if (!verificationSuccess)
        {
            _logger.LogWarning("Email verification failed for user {UserId} with token {Token}. Token might be expired or mismatched.", user.Id, request.Token);
            return Result.Failure("User.VerifyEmail.Failed", "Email verification failed. The token may be invalid or expired.");
        }

        try
        {
            // _userRepository.Update(user); // EF Core tracks changes
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Email successfully verified for user {UserId}.", user.Id);
            return Result.Success();
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error saving email verification status for user {UserId}.", user.Id);
            return Result.Failure("User.VerifyEmail.StorageError", "An error occurred while finalizing email verification.");
        }
    }
}