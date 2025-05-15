using IMSystem.Protocol.DTOs.Responses.Friends; // Assuming a FriendshipDto or similar exists
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.Friends.Queries
{
    // It might be more appropriate to return a more detailed DTO than FriendRequestDto,
    // e.g., a FriendshipDetailDto that includes user details of both parties.
    // For now, let's assume we might reuse FriendRequestDto or a new specific DTO.
    // Let's define it to return a generic FriendDto for now, which can be specialized.
    public class GetFriendshipByIdQuery : IRequest<FriendDto?>
    {
        public Guid FriendshipId { get; }
        public Guid RequesterId { get; } // To verify if the requester is part of this friendship

        public GetFriendshipByIdQuery(Guid friendshipId, Guid requesterId)
        {
            if (friendshipId == Guid.Empty)
            {
                throw new ArgumentException("Friendship ID cannot be empty.", nameof(friendshipId));
            }
            if (requesterId == Guid.Empty)
            {
                throw new ArgumentException("Requester ID cannot be empty.", nameof(requesterId));
            }
            FriendshipId = friendshipId;
            RequesterId = requesterId;
        }
    }
}