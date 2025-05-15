using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.ValueObjects; // For Address
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.User.Commands;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

    public UpdateUserProfileCommandHandler(
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateUserProfileCommandHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to update profile for User ID: {UserId}", request.UserId);

        var userProfile = await _userProfileRepository.GetByUserIdAsync(request.UserId);

        if (userProfile == null)
        {
            _logger.LogWarning("User profile not found for User ID: {UserId}", request.UserId);
            return Result.Failure("UserProfile.NotFound", $"User profile not found for User ID {request.UserId}.");
        }

        try
        {
            Domain.ValueObjects.Address? newAddress = null;
            if (!string.IsNullOrWhiteSpace(request.Street) ||
                !string.IsNullOrWhiteSpace(request.City) ||
                !string.IsNullOrWhiteSpace(request.StateOrProvince) ||
                !string.IsNullOrWhiteSpace(request.Country) ||
                !string.IsNullOrWhiteSpace(request.ZipCode))
            {
                // Only create Address object if at least one field is provided.
                // The Address.Create factory method will validate individual fields.
                // Handle potential ArgumentException from Address.Create if fields are invalid (e.g. all null/whitespace passed to it, though we check above)
                try
                {
                    newAddress = Domain.ValueObjects.Address.Create(
                        request.Street ?? string.Empty, // Pass empty string if null, Address.Create handles validation
                        request.City ?? string.Empty,
                        request.StateOrProvince ?? string.Empty,
                        request.Country ?? string.Empty,
                        request.ZipCode ?? string.Empty
                    );
                }
                catch (ArgumentException argEx)
                {
                    // This can happen if all parts of address are effectively empty but an attempt to create Address was made.
                    // Or if Address.Create has stricter rules not caught by the IsNullOrWhiteSpace check above.
                     _logger.LogWarning("Invalid address components provided for User ID {UserId}: {ErrorMessage}", request.UserId, argEx.Message);
                    // Depending on desired behavior, either return failure or proceed with newAddress as null.
                    // For now, let's treat invalid address components (if Address.Create throws) as a bad request.
                    // However, the UserProfile.UpdateDetails expects a nullable Address, so if all components are null/empty,
                    // newAddress should remain null and that's fine. The issue is if Address.Create itself throws.
                    // A more robust check would be to only call Address.Create if ALL required parts are present.
                    // For now, if Address.Create throws, we'll let it bubble or catch and return failure.
                    // Let's assume if any part is given, the user intends to set/update the address.
                    // If Address.Create throws due to *all* parts being empty (which our IsNullOrWhiteSpace check above tries to prevent for *creating* newAddress),
                    // then newAddress would remain null.
                    // If Address.Create throws for other reasons (e.g. invalid format for a specific part if Address had such validation),
                    // then it's a validation issue.
                    // The current Address.Create throws if any part is null/whitespace.
                    // So, we must ensure we only call it if we have *valid* parts.
                    // The logic above is: if *any* part is non-whitespace, we try to create.
                    // This means if only Street is "123", City etc are null, Address.Create will fail.
                    // This needs refinement.

                    // Refined logic: Only attempt to create Address if all *required* parts for a valid Address are present.
                    // Or, make Address constructor/Create method more lenient and allow partial addresses.
                    // For now, if any address part is given, we try to form an Address.
                    // If it fails (e.g. only street provided), it's a bad request.
                    // The UserProfile.UpdateDetails method takes a nullable Address.
                    // If all request.Street etc. are null, newAddress will be null, and UpdateDetails will clear the address.
                    // If some are provided but not enough for Address.Create, then it's an issue.

                    // Let's refine: if any address field is non-empty, we expect all *required by Address.Create* to be non-empty.
                    // Address.Create requires all its parameters to be non-null/whitespace.
                    // So, if the user provides *any* address field, they must provide *all* of them.
                    // This is a strict interpretation. A more flexible one would allow partial updates.

                    // Simpler approach for now: if any address field is provided, try to create. If Address.Create fails, return bad request.
                    // If all address fields from request are null/whitespace, newAddress remains null.
                    _logger.LogWarning(argEx, "Failed to create Address object from request for User ID {UserId}.", request.UserId);
                    return Result.Failure("UserProfile.Address.Invalid", "Invalid address information provided. All address fields (Street, City, State, Country, ZipCode) are required if an address is being specified.");
                }
            }


            // Gender is now GenderType? in command and entity, no conversion needed here.

            // Convert DateOfBirth (DateOnly?) to DateTime? for UserProfile.UpdateDetails
            DateTime? dateOfBirthDateTime = request.DateOfBirth.HasValue
                ? request.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue)
                : null;

            userProfile.UpdateDetails(
                request.Nickname,
                request.AvatarUrl,
                request.Gender, // Pass GenderType? directly
                dateOfBirthDateTime, // Pass converted DateTime?
                newAddress, // Pass the new Address object or null
                request.Bio,
                request.UserId // ModifierId is the user themselves
            );

            // _userProfileRepository.Update(userProfile); // Not strictly necessary if using EF Core change tracking and profile is tracked
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated profile for User ID: {UserId}", request.UserId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for User ID: {UserId}", request.UserId);
            return Result.Failure("UserProfile.UpdateError", $"An error occurred while updating the profile: {ex.Message}");
        }
    }
}