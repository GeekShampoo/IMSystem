using MediatR;
using IMSystem.Protocol.Common;
using System;

namespace IMSystem.Server.Core.Features.Group.Queries
{
    public record IsUserMemberOfGroupQuery(Guid UserId, Guid GroupId) : IRequest<Result<bool>>;
}