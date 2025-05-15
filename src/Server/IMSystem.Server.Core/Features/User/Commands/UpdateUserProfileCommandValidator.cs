using FluentValidation;
using System;
using IMSystem.Protocol.Enums; // Added for ProtocolGender

namespace IMSystem.Server.Core.Features.User.Commands;

public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID cannot be empty.");

        RuleFor(x => x.Nickname)
            .MaximumLength(100).WithMessage("Nickname cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Nickname)); // Only validate if provided

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(2048).WithMessage("Avatar URL cannot exceed 2048 characters.")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl))
            .WithMessage("Avatar URL must be a valid absolute URL.");

        // x.Gender is now ProtocolGender?
        // MaximumLength is not applicable to enum types.
        // If validation is needed (e.g., to ensure it's a defined enum value and not null if provided),
        // FluentValidation typically handles basic enum validation.
        // We can ensure it's a valid enum value if provided.
        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender value.")
            .When(x => x.Gender.HasValue); // Only validate if a gender is provided

        // x.DateOfBirth is now DateOnly?
        RuleFor(x => x.DateOfBirth)
            // Compare with DateOnly values
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-5)).WithMessage("User must be at least 5 years old.")
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-120)).WithMessage("Date of birth is unrealistic.")
            .When(x => x.DateOfBirth.HasValue);

        // Removed RuleFor(x => x.Region)

        // Add validation for new address fields (optional)
        // Assuming Address ValueObject has its own internal validation for consistency if created.
        // Here, we can validate the input strings if provided.
        RuleFor(x => x.Street)
            .MaximumLength(200).WithMessage("Street address cannot exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Street));

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.StateOrProvince)
            .MaximumLength(100).WithMessage("State or province cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.StateOrProvince));

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.Country));

        RuleFor(x => x.ZipCode)
            .MaximumLength(20).WithMessage("Zip code cannot exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.ZipCode));
            // Add more specific zip code validation if needed (e.g., regex for a specific country)

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Bio cannot exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Bio));
    }
}