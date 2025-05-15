using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Groups;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For GroupMember
using IMSystem.Server.Domain.Enums;   // For GroupMemberRole
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMSystem.Server.Core.Features.Groups.Queries;

public class GetSentGroupInvitationsQueryHandler : IRequestHandler<GetSentGroupInvitationsQuery, Result<IEnumerable<GroupInvitationDto>>>
{
    private readonly IGroupInvitationRepository _groupInvitationRepository;
    private readonly IGroupRepository _groupRepository; // To check group existence and owner
    private readonly IGroupMemberRepository _groupMemberRepository; // To check if requestor is an admin/owner
    private readonly IMapper _mapper;
    private readonly ILogger<GetSentGroupInvitationsQueryHandler> _logger;

    public GetSentGroupInvitationsQueryHandler(
        IGroupInvitationRepository groupInvitationRepository,
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IMapper mapper,
        ILogger<GetSentGroupInvitationsQueryHandler> logger)
    {
        _groupInvitationRepository = groupInvitationRepository;
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IEnumerable<GroupInvitationDto>>> Handle(GetSentGroupInvitationsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {RequestorUserId} attempting to fetch sent invitations for group {GroupId}", request.RequestorUserId, request.GroupId);

        var group = await _groupRepository.GetByIdAsync(request.GroupId);
        if (group == null)
        {
            _logger.LogWarning("Get sent invitations failed: Group {GroupId} not found.", request.GroupId);
            return Result<IEnumerable<GroupInvitationDto>>.Failure("Group.NotFound", $"群组 {request.GroupId} 不存在。");
        }

        // Permission check: Only owner or admin can view sent invitations for the group
        bool hasPermission = false;
        if (group.OwnerId == request.RequestorUserId)
        {
            hasPermission = true;
        }
        else
        {
            var requestorMembership = await _groupMemberRepository.GetMemberOrDefaultAsync(request.GroupId, request.RequestorUserId);
            if (requestorMembership != null &&
                (requestorMembership.Role == GroupMemberRole.Admin || requestorMembership.Role == GroupMemberRole.Owner)) // Owner check is redundant if group.OwnerId is checked
            {
                hasPermission = true;
            }
        }

        if (!hasPermission)
        {
            _logger.LogWarning("User {RequestorUserId} does not have permission to view sent invitations for group {GroupId}.", request.RequestorUserId, request.GroupId);
            return Result<IEnumerable<GroupInvitationDto>>.Failure("GroupInvitation.ViewSent.AccessDenied", "您没有权限查看此群组发送的邀请列表。");
        }

        try
        {
            var invitations = await _groupInvitationRepository.GetInvitationsSentByGroupAsync(request.GroupId);

            if (invitations == null || !invitations.Any())
            {
                _logger.LogInformation("No invitations found sent by group {GroupId}", request.GroupId);
                return Result<IEnumerable<GroupInvitationDto>>.Success(new List<GroupInvitationDto>());
            }

            var dtos = _mapper.Map<IEnumerable<GroupInvitationDto>>(invitations);

            _logger.LogInformation("Successfully fetched {Count} invitations sent by group {GroupId} for user {RequestorUserId}",
                dtos.Count(), request.GroupId, request.RequestorUserId);
            return Result<IEnumerable<GroupInvitationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching invitations sent by group {GroupId} for user {RequestorUserId}", request.GroupId, request.RequestorUserId);
            return Result<IEnumerable<GroupInvitationDto>>.Failure("GroupInvitation.FetchSent.Error", $"获取群组已发送邀请列表时发生错误: {ex.Message}");
        }
    }
}