using IMSystem.Protocol.DTOs.Responses.User;
using MediatR;
using IMSystem.Protocol.Common;
using System;

namespace IMSystem.Server.Core.Features.User.Queries
{
    public class GetUserByIdQuery : IRequest<Result<UserDto?>>
    {
        public Guid UserId { get; }

        public GetUserByIdQuery(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty.", nameof(userId));
            }
            UserId = userId;
        }
    }
}