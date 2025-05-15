using AutoMapper;
using IMSystem.Protocol.DTOs.Responses.Friends;
using IMSystem.Protocol.Enums; // Added for ProtocolFriendStatus
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Enums; // Added for Domain FriendshipStatus
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Friends.Queries
{
    public class GetFriendshipByIdQueryHandler : IRequestHandler<GetFriendshipByIdQuery, FriendDto?>
    {
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserRepository _userRepository; // To fetch user details for the DTO
        private readonly IMapper _mapper;
        private readonly ILogger<GetFriendshipByIdQueryHandler> _logger;

        public GetFriendshipByIdQueryHandler(
            IFriendshipRepository friendshipRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<GetFriendshipByIdQueryHandler> logger)
        {
            _friendshipRepository = friendshipRepository ?? throw new ArgumentNullException(nameof(friendshipRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FriendDto?> Handle(GetFriendshipByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to retrieve friendship with ID: {FriendshipId} for Requester: {RequesterId}", request.FriendshipId, request.RequesterId);

            var friendship = await _friendshipRepository.GetByIdAsync(request.FriendshipId);

            if (friendship == null)
            {
                _logger.LogWarning("Friendship with ID: {FriendshipId} not found.", request.FriendshipId);
                return null;
            }

            // Verify if the requester is part of this friendship
            if (friendship.RequesterId != request.RequesterId && friendship.AddresseeId != request.RequesterId)
            {
                _logger.LogWarning("Requester {RequesterId} is not part of friendship {FriendshipId} (Requester: {FRequesterId}, Addressee: {FAddresseeId}).",
                    request.RequesterId, request.FriendshipId, friendship.RequesterId, friendship.AddresseeId);
                // Or throw UnauthorizedAccessException
                return null;
            }

            // Determine the "friend" user for the requester
            var friendUserId = friendship.RequesterId == request.RequesterId ? friendship.AddresseeId : friendship.RequesterId;
            var friendUser = await _userRepository.GetByIdAsync(friendUserId);

            if (friendUser == null)
            {
                _logger.LogError("Could not find friend user with ID: {FriendUserId} for friendship {FriendshipId}.", friendUserId, request.FriendshipId);
                // This case should ideally not happen if data integrity is maintained.
                return null; // Or handle as a server error
            }
            
            // Map to FriendDto. This DTO should represent the "other" person in the friendship from the requester's perspective.
            // The FriendDto might need properties like UserId, Username, Nickname, ProfilePictureUrl, FriendshipStatus, FriendshipId
            var friendDto = _mapper.Map<FriendDto>(friendUser); // Assuming FriendDto can be mapped from User
            friendDto.FriendshipId = friendship.Id;

            // Convert Domain.FriendshipStatus to Protocol.ProtocolFriendStatus
            switch (friendship.Status)
            {
                case Domain.Enums.FriendshipStatus.Accepted:
                    friendDto.Status = ProtocolFriendStatus.Friends;
                    break;
                case Domain.Enums.FriendshipStatus.Pending:
                    if (friendship.RequesterId == request.RequesterId)
                    {
                        friendDto.Status = ProtocolFriendStatus.PendingOutgoing;
                    }
                    else if (friendship.AddresseeId == request.RequesterId)
                    {
                        friendDto.Status = ProtocolFriendStatus.PendingIncoming;
                    }
                    else
                    {
                        // Should not happen if requester is part of the friendship
                        friendDto.Status = ProtocolFriendStatus.None;
                    }
                    break;
                case Domain.Enums.FriendshipStatus.Blocked:
                    // LastModifiedBy indicates who performed the block action.
                    // If the current requester was the one who last modified (blocked), it's BlockedBySelf.
                    if (friendship.LastModifiedBy == request.RequesterId)
                    {
                        friendDto.Status = ProtocolFriendStatus.BlockedBySelf;
                    }
                    else
                    {
                        friendDto.Status = ProtocolFriendStatus.BlockedByOther;
                    }
                    break;
                case Domain.Enums.FriendshipStatus.Declined:
                    friendDto.Status = ProtocolFriendStatus.None; // Or handle as per specific business logic for declined requests in DTO
                    break;
                default:
                    friendDto.Status = ProtocolFriendStatus.None; // Default or unknown status
                    break;
            }
            
            // friendDto.FriendSince = friendship.Status == FriendshipStatus.Accepted ? friendship.LastModifiedAt : null; // Or a specific AcceptedAt field

            _logger.LogInformation("Successfully retrieved friendship with ID: {FriendshipId}. Mapping to FriendDto for user {FriendUserId}.", request.FriendshipId, friendUserId);
            return friendDto;
        }
    }
}