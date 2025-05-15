using AutoMapper;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Friends;
using IMSystem.Protocol.Enums; // Added for ProtocolFriendStatus
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For User entity
using IMSystem.Server.Domain.Enums;   // For FriendshipStatus
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Friends.Queries
{
    public class GetFriendsQueryHandler : IRequestHandler<GetFriendsQuery, Result<PagedResult<FriendDto>>>
    {
        private readonly IFriendshipRepository _friendshipRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetFriendsQueryHandler> _logger;

        public GetFriendsQueryHandler(
            IFriendshipRepository friendshipRepository,
            IUserRepository userRepository,
            IMapper mapper,
            ILogger<GetFriendsQueryHandler> logger)
        {
            _friendshipRepository = friendshipRepository ?? throw new ArgumentNullException(nameof(friendshipRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<PagedResult<FriendDto>>> Handle(GetFriendsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to retrieve friends for user ID: {UserId}", request.CurrentUserId);

            // 1. 获取总数
            var allFriendships = await _friendshipRepository.GetUserFriendshipsAsync(request.CurrentUserId, FriendshipStatus.Accepted);
            var totalCount = allFriendships?.Count() ?? 0;

            // 2. 分页获取
            var pagedFriendships = await _friendshipRepository.GetUserFriendshipsAsync(
                request.CurrentUserId,
                FriendshipStatus.Accepted,
                request.PageNumber,
                request.PageSize
            );

            if (pagedFriendships == null || !pagedFriendships.Any())
            {
                _logger.LogInformation("No accepted friendships found for user ID: {UserId}", request.CurrentUserId);
                return Result<PagedResult<FriendDto>>.Success(PagedResult<FriendDto>.Success(new List<FriendDto>(), totalCount, request.PageNumber, request.PageSize));
            }

            var friendUserIds = pagedFriendships
                .Select(f => f.RequesterId == request.CurrentUserId ? f.AddresseeId : f.RequesterId)
                .Distinct()
                .ToList();

            if (!friendUserIds.Any())
            {
                return Result<PagedResult<FriendDto>>.Success(PagedResult<FriendDto>.Success(new List<FriendDto>(), totalCount, request.PageNumber, request.PageSize));
            }

            // 批量查用户详情
            var friendUsers = await _userRepository.GetUsersByExternalIdsWithProfileAsync(friendUserIds);
            var friendUsersMap = friendUsers.ToDictionary(u => u.Id);

            var friendDtos = new List<FriendDto>();
            foreach (var friendship in pagedFriendships)
            {
                var friendUserId = friendship.RequesterId == request.CurrentUserId ? friendship.AddresseeId : friendship.RequesterId;

                if (friendUsersMap.TryGetValue(friendUserId, out var friendUser))
                {
                    var friendDto = _mapper.Map<FriendDto>(friendUser);
                    friendDto.FriendshipId = friendship.Id;
                    friendDto.Status = ProtocolFriendStatus.Friends;

                    // 备注名
                    if (request.CurrentUserId == friendship.RequesterId)
                        friendDto.RemarkName = friendship.RequesterRemark;
                    else if (request.CurrentUserId == friendship.AddresseeId)
                        friendDto.RemarkName = friendship.AddresseeRemark;

                    friendDtos.Add(friendDto);
                }
                else
                {
                    _logger.LogWarning("Could not find user details for friend ID: {FriendId} (from batch fetch) related to friendship ID: {FriendshipId}", friendUserId, friendship.Id);
                }
            }

            _logger.LogInformation("Successfully retrieved {Count} friends for user ID: {UserId}", friendDtos.Count, request.CurrentUserId);
            return Result<PagedResult<FriendDto>>.Success(PagedResult<FriendDto>.Success(friendDtos, totalCount, request.PageNumber, request.PageSize));
        }
    }
}