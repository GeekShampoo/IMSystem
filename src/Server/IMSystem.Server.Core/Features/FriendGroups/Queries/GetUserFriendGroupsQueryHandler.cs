using AutoMapper;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.FriendGroups;
using IMSystem.Protocol.DTOs.Responses.Friends; // For FriendSummaryDto
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For User entity etc.
using IMSystem.Server.Domain.Enums;   // For FriendshipStatus
using MediatR;
using Microsoft.Extensions.Logging;
using System; // For Guid
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.FriendGroups.Queries;

public class GetUserFriendGroupsQueryHandler : IRequestHandler<GetUserFriendGroupsQuery, Result<IEnumerable<FriendGroupDto>>>
{
    private readonly IFriendGroupRepository _friendGroupRepository;
    private readonly IUserFriendGroupRepository _userFriendGroupRepository; // Added
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserFriendGroupsQueryHandler> _logger;

    public GetUserFriendGroupsQueryHandler(
        IFriendGroupRepository friendGroupRepository,
        IUserFriendGroupRepository userFriendGroupRepository, // Added
        IMapper mapper,
        ILogger<GetUserFriendGroupsQueryHandler> logger)
    {
        _friendGroupRepository = friendGroupRepository;
        _userFriendGroupRepository = userFriendGroupRepository; // Added
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<FriendGroupDto>>> Handle(GetUserFriendGroupsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("尝试获取用户 {UserId} 的所有好友分组列表 (包含好友信息)。", request.CurrentUserId);

        var friendGroupEntities = await _friendGroupRepository.GetByUserIdAsync(request.CurrentUserId);

        if (friendGroupEntities == null || !friendGroupEntities.Any())
        {
            _logger.LogInformation("用户 {UserId} 没有任何好友分组。", request.CurrentUserId);
            return Result<IEnumerable<FriendGroupDto>>.Success(Enumerable.Empty<FriendGroupDto>());
        }

        var resultList = new List<FriendGroupDto>();

        foreach (var fgEntity in friendGroupEntities)
        {
            // 1. Map FriendGroup entity to FriendGroupDto (basic properties)
            var groupDto = _mapper.Map<FriendGroupDto>(fgEntity);
            // IsDefault should be mapped by AutoMapper if FriendGroupDto has it and FriendGroup entity has it.

            // 2. Get friends in this group
            var ufgLinks = await _userFriendGroupRepository.GetByFriendGroupIdAsync(fgEntity.Id);
            
            foreach (var ufgLink in ufgLinks)
            {
                if (ufgLink.Friendship == null)
                {
                    _logger.LogWarning("UserFriendGroup (ID: {UserFriendGroupId}) 关联的 Friendship 为空，FriendshipId: {FriendshipId}。跳过此好友。",
                        ufgLink.Id, ufgLink.FriendshipId);
                    continue;
                }

                var friendshipEntity = ufgLink.Friendship;

                // Skip if friendship is not accepted
                if (friendshipEntity.Status != FriendshipStatus.Accepted)
                {
                     _logger.LogInformation("Friendship (ID: {FriendshipId}) 状态为 {Status}，非Accepted。不作为好友显示在分组 {GroupId} 中。",
                        friendshipEntity.Id, friendshipEntity.Status, fgEntity.Id);
                    continue;
                }

                Domain.Entities.User? friendUserEntity = null;
                string? remarkName = null;

                if (friendshipEntity.RequesterId == request.CurrentUserId)
                {
                    friendUserEntity = friendshipEntity.Addressee;
                    remarkName = friendshipEntity.RequesterRemark; // Current user's remark on Addressee
                }
                else if (friendshipEntity.AddresseeId == request.CurrentUserId)
                {
                    friendUserEntity = friendshipEntity.Requester;
                    remarkName = friendshipEntity.AddresseeRemark; // Current user's remark on Requester
                }
                else
                {
                    // This case should ideally not happen if data integrity is maintained
                    _logger.LogError("Friendship (ID: {FriendshipId}) 与当前用户 {CurrentUserId} 无直接关联 (Requester: {RequesterId}, Addressee: {AddresseeId})。无法确定好友信息。",
                        friendshipEntity.Id, request.CurrentUserId, friendshipEntity.RequesterId, friendshipEntity.AddresseeId);
                    continue;
                }

                if (friendUserEntity == null)
                {
                    _logger.LogWarning("Friendship (ID: {FriendshipId}) 关联的好友用户信息为空。跳过此好友。", friendshipEntity.Id);
                    continue;
                }

                var friendSummary = new FriendSummaryDto
                {
                    FriendUserId = friendUserEntity.Id,
                    FriendshipId = friendshipEntity.Id,
                    Username = friendUserEntity.Username,
                    Nickname = friendUserEntity.Profile?.Nickname,
                    AvatarUrl = friendUserEntity.Profile?.AvatarUrl,
                    RemarkName = remarkName,
                    IsOnline = friendUserEntity.IsOnline, // Corrected property name and source
                    CustomStatus = friendUserEntity.CustomStatus,
                    LastSeenAt = friendUserEntity.LastSeenAt
                };
                groupDto.Friends.Add(friendSummary);
            }
            resultList.Add(groupDto);
        }
        
        _logger.LogInformation("成功获取用户 {UserId} 的 {Count} 个好友分组，并填充了好友信息。", request.CurrentUserId, resultList.Count);
        return Result<IEnumerable<FriendGroupDto>>.Success(resultList);
    }
}