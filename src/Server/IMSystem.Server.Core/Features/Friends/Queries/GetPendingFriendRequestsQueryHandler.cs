using AutoMapper;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Friends;
using IMSystem.Server.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Friends.Queries;

public class GetPendingFriendRequestsQueryHandler : IRequestHandler<GetPendingFriendRequestsQuery, Result<IEnumerable<FriendRequestDto>>>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPendingFriendRequestsQueryHandler> _logger;

    public GetPendingFriendRequestsQueryHandler(
        IFriendshipRepository friendshipRepository,
        IMapper mapper,
        ILogger<GetPendingFriendRequestsQueryHandler> logger)
    {
        _friendshipRepository = friendshipRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<FriendRequestDto>>> Handle(GetPendingFriendRequestsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("尝试获取用户 {UserId} 的待处理好友请求列表。", request.CurrentUserId);

        var pendingRequests = await _friendshipRepository.GetPendingFriendRequestsForUserAsync(request.CurrentUserId);

        if (pendingRequests == null || !pendingRequests.Any())
        {
            _logger.LogInformation("用户 {UserId} 没有待处理的好友请求。", request.CurrentUserId);
            return Result<IEnumerable<FriendRequestDto>>.Success(Enumerable.Empty<FriendRequestDto>());
        }

        // 使用 AutoMapper 将 Friendship 实体集合映射到 FriendRequestDto 集合
        // 这需要在 MappingProfile 中配置 Friendship -> FriendRequestDto 的映射
        var dtos = _mapper.Map<IEnumerable<FriendRequestDto>>(pendingRequests);
        
        _logger.LogInformation("成功获取用户 {UserId} 的 {Count} 条待处理好友请求。", request.CurrentUserId, dtos.Count());
        return Result<IEnumerable<FriendRequestDto>>.Success(dtos);
    }
}