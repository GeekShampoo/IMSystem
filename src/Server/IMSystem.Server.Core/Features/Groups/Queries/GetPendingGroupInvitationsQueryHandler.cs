using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Groups;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // Required for GroupInvitation
using IMSystem.Server.Domain.Enums; // Required for GroupInvitationStatus
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMSystem.Server.Core.Features.Groups.Queries;

public class GetPendingGroupInvitationsQueryHandler : IRequestHandler<GetPendingGroupInvitationsQuery, Result<IEnumerable<GroupInvitationDto>>>
{
    private readonly IGroupInvitationRepository _groupInvitationRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetPendingGroupInvitationsQueryHandler> _logger;

    public GetPendingGroupInvitationsQueryHandler(
        IGroupInvitationRepository groupInvitationRepository,
        IMapper mapper,
        ILogger<GetPendingGroupInvitationsQueryHandler> logger)
    {
        _groupInvitationRepository = groupInvitationRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<GroupInvitationDto>>> Handle(GetPendingGroupInvitationsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching pending group invitations for user {UserId}", request.UserId);

        try
        {
            // The repository method should ideally handle filtering by Pending status and non-expired.
            // If not, additional filtering might be needed here.
            var invitations = await _groupInvitationRepository.GetPendingInvitationsForUserAsync(request.UserId);

            if (invitations == null || !invitations.Any())
            {
                _logger.LogInformation("No pending group invitations found for user {UserId}", request.UserId);
                return Result<IEnumerable<GroupInvitationDto>>.Success(new List<GroupInvitationDto>());
            }

            // Filter for pending and not expired, if not already handled by the repository method.
            // This is a good place for a sanity check.
            var validPendingInvitations = invitations
                .Where(inv => inv.Status == GroupInvitationStatus.Pending && 
                              (!inv.ExpiresAt.HasValue || inv.ExpiresAt.Value > DateTime.UtcNow))
                .ToList();
            
            if (!validPendingInvitations.Any())
            {
                 _logger.LogInformation("No valid (pending and not expired) group invitations found for user {UserId} after filtering.", request.UserId);
                return Result<IEnumerable<GroupInvitationDto>>.Success(new List<GroupInvitationDto>());
            }

            // Manual mapping example if AutoMapper profile is not set up yet for GroupInvitation -> GroupInvitationDto
            // var dtos = validPendingInvitations.Select(inv => new GroupInvitationDto
            // {
            //     InvitationId = inv.Id,
            //     GroupId = inv.GroupId,
            //     GroupName = inv.Group?.Name ?? "N/A", // Requires Group to be loaded
            //     GroupAvatarUrl = inv.Group?.AvatarUrl,
            //     InviterId = inv.InviterId,
            //     InviterUsername = inv.Inviter?.Username ?? "N/A", // Requires Inviter to be loaded
            //     InviterNickname = inv.Inviter?.Nickname,
            //     InviterAvatarUrl = inv.Inviter?.ProfilePictureUrl,
            //     InvitedUserId = inv.InvitedUserId,
            //     InvitedUsername = inv.InvitedUser?.Username ?? "N/A", // Requires InvitedUser to be loaded
            //     Status = inv.Status.ToString(),
            //     Message = inv.Message,
            //     CreatedAt = inv.CreatedAt,
            //     ExpiresAt = inv.ExpiresAt
            // }).ToList();
            // For AutoMapper to work effectively, ensure navigation properties (Group, Inviter, InvitedUser) are loaded
            // by GetPendingInvitationsForUserAsync or configure AutoMapper to handle null navigation properties gracefully.

            var dtos = _mapper.Map<IEnumerable<GroupInvitationDto>>(validPendingInvitations);

            _logger.LogInformation("Successfully fetched {Count} pending group invitations for user {UserId}", dtos.Count(), request.UserId);
            return Result<IEnumerable<GroupInvitationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending group invitations for user {UserId}", request.UserId);
            return Result<IEnumerable<GroupInvitationDto>>.Failure("GroupInvitation.FetchPending.Error", $"获取待处理群组邀请时发生错误: {ex.Message}");
        }
    }
}