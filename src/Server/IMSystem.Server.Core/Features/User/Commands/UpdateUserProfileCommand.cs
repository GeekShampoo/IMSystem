using System;
using MediatR;
using IMSystem.Protocol.Common; // For Result
using IMSystem.Server.Domain.Enums; // For GenderType

namespace IMSystem.Server.Core.Features.User.Commands;

/// <summary>
/// Command to update a user's profile.
/// </summary>
public record UpdateUserProfileCommand(
    Guid UserId, // The ID of the user whose profile is being updated
    string? Nickname,
    string? AvatarUrl,
    GenderType? Gender,
    DateOnly? DateOfBirth,    // Changed from DateTime?
    // string? Region, // Replaced by structured address fields
    string? Street,
    string? City,
    string? StateOrProvince,
    string? Country,
    string? ZipCode,
    string? Bio
) : IRequest<Result>; // Returns a simple Result indicating success or failure