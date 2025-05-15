using IMSystem.Protocol.Common;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.User.Commands;

public class UpdateUserCustomStatusCommand : IRequest<Result>
{
    public Guid UserId { get; }
    public string? CustomStatus { get; }

    public UpdateUserCustomStatusCommand(Guid userId, string? customStatus)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }
        UserId = userId;
        CustomStatus = customStatus; // Can be null or empty
    }
}