using IMSystem.Protocol.DTOs.Responses.FriendGroups;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.FriendGroups.Queries
{
    public class GetFriendGroupByIdQuery : IRequest<FriendGroupDto?>
    {
        public Guid GroupId { get; }
        public Guid RequesterId { get; } // 用于验证用户是否有权查看该分组

        public GetFriendGroupByIdQuery(Guid groupId, Guid requesterId)
        {
            if (groupId == Guid.Empty)
            {
                throw new ArgumentException("Group ID cannot be empty.", nameof(groupId));
            }
            if (requesterId == Guid.Empty)
            {
                throw new ArgumentException("Requester ID cannot be empty.", nameof(requesterId));
            }
            GroupId = groupId;
            RequesterId = requesterId;
        }
    }
}