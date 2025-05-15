using AutoMapper;
using IMSystem.Protocol.DTOs.Responses.FriendGroups;
using IMSystem.Server.Core.Interfaces.Persistence;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.FriendGroups.Queries
{
    public class GetFriendGroupByIdQueryHandler : IRequestHandler<GetFriendGroupByIdQuery, FriendGroupDto?>
    {
        private readonly IFriendGroupRepository _friendGroupRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetFriendGroupByIdQueryHandler> _logger;

        public GetFriendGroupByIdQueryHandler(
            IFriendGroupRepository friendGroupRepository,
            IMapper mapper,
            ILogger<GetFriendGroupByIdQueryHandler> logger)
        {
            _friendGroupRepository = friendGroupRepository ?? throw new ArgumentNullException(nameof(friendGroupRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<FriendGroupDto?> Handle(GetFriendGroupByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to retrieve friend group with ID: {GroupId} for Requester: {RequesterId}", request.GroupId, request.RequesterId);

            var friendGroup = await _friendGroupRepository.GetByIdAsync(request.GroupId);

            if (friendGroup == null)
            {
                _logger.LogWarning("Friend group with ID: {GroupId} not found.", request.GroupId);
                return null;
            }

            // 验证请求者是否有权查看此分组
            if (friendGroup.CreatedBy != request.RequesterId)
            {
                _logger.LogWarning("Requester {RequesterId} is not authorized to view friend group {GroupId} owned by {OwnerId}.",
                    request.RequesterId, request.GroupId, friendGroup.CreatedBy);
                // 或者可以抛出一个 UnauthorizedAccessException，然后在 Controller 层捕获并返回 403 Forbidden
                return null; // 或者根据策略返回特定的错误响应或 DTO
            }

            _logger.LogInformation("Successfully retrieved friend group with ID: {GroupId}. Mapping to FriendGroupDto.", request.GroupId);
            return _mapper.Map<FriendGroupDto>(friendGroup);
        }
    }
}