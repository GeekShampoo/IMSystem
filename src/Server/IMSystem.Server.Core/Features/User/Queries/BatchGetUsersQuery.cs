using MediatR;
using System.Collections.Generic;
using IMSystem.Protocol.DTOs.Responses.User;
using IMSystem.Protocol.Common; // For Result<T>
using System; // For Guid

namespace IMSystem.Server.Core.Features.User.Queries
{
    public record BatchGetUsersQuery(List<Guid> UserExternalIds) : IRequest<Result<List<UserSummaryDto>>>;
}