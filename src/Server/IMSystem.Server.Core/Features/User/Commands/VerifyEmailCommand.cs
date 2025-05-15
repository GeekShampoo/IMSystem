using IMSystem.Protocol.Common;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace IMSystem.Server.Core.Features.User.Commands;

/// <summary>
/// Command to verify a user's email address using a token.
/// </summary>
public class VerifyEmailCommand : IRequest<Result>
{
    /// <summary>
    /// The email verification token.
    /// </summary>
    [Required]
    public string Token { get; }

    public VerifyEmailCommand(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Verification token cannot be empty.", nameof(token));
        }
        Token = token;
    }
}