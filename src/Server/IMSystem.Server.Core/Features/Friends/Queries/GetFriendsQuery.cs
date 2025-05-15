using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Friends;
using MediatR;
using System;
using System.Collections.Generic;

namespace IMSystem.Server.Core.Features.Friends.Queries
{
    public class GetFriendsQuery : IRequest<Result<PagedResult<FriendDto>>>
    {
        public Guid CurrentUserId { get; }
        public int PageNumber { get; }
        public int PageSize { get; }

        public GetFriendsQuery(Guid currentUserId, int pageNumber = 1, int pageSize = 20)
        {
            if (currentUserId == Guid.Empty)
            {
                throw new ArgumentException("Current User ID cannot be empty.", nameof(currentUserId));
            }
            if (pageNumber <= 0) throw new ArgumentException("PageNumber must be greater than 0.", nameof(pageNumber));
            if (pageSize <= 0) throw new ArgumentException("PageSize must be greater than 0.", nameof(pageSize));
            CurrentUserId = currentUserId;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}