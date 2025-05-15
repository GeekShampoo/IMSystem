using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For GroupMember, GroupInvitation
using IMSystem.Server.Domain.Enums;
using IMSystem.Server.Domain.Events.Groups; // Added for GroupInvitationCancelledEvent
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class CancelGroupInvitationCommandHandler : IRequestHandler<CancelGroupInvitationCommand, Result>
{
    private readonly IGroupInvitationRepository _groupInvitationRepository;
    private readonly IGroupRepository _groupRepository; 
    private readonly IGroupMemberRepository _groupMemberRepository; 
    private readonly IUserRepository _userRepository; // Added to get canceller's username
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelGroupInvitationCommandHandler> _logger;

    public CancelGroupInvitationCommandHandler(
        IGroupInvitationRepository groupInvitationRepository,
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IUserRepository userRepository, // Added
        IUnitOfWork unitOfWork,
        ILogger<CancelGroupInvitationCommandHandler> logger)
    {
        _groupInvitationRepository = groupInvitationRepository;
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _userRepository = userRepository; // Added
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(CancelGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {CancellerUserId} attempting to cancel group invitation {InvitationId}", request.CancellerUserId, request.InvitationId);

        var invitation = await _groupInvitationRepository.GetByIdAsync(request.InvitationId); // Assumes Group, Inviter, InvitedUser are included

        if (invitation == null)
        {
            _logger.LogWarning("Cancel invitation failed: Invitation {InvitationId} not found.", request.InvitationId);
            return Result.Failure("GroupInvitation.NotFound", $"邀请 {request.InvitationId} 不存在。");
        }

        // Ensure related entities for the event are loaded
        if (invitation.Group == null)
        {
            _logger.LogError("Data integrity issue: Group {GroupId} for invitation {InvitationId} not found (not included or deleted).", invitation.GroupId, invitation.Id);
            return Result.Failure("Group.NotFound", $"邀请关联的群组 {invitation.GroupId} 不存在，无法取消。");
        }
        if (invitation.InvitedUser == null)
        {
            _logger.LogError("Data integrity issue: InvitedUser {InvitedUserId} for invitation {InvitationId} not found (not included or deleted).", invitation.InvitedUserId, invitation.Id);
            return Result.Failure("User.NotFound", $"邀请关联的用户 {invitation.InvitedUserId} 不存在，无法取消。");
        }

        var cancellerUser = await _userRepository.GetByIdAsync(request.CancellerUserId);
        if (cancellerUser == null)
        {
            _logger.LogError("Cancel invitation failed: Canceller user {CancellerUserId} not found.", request.CancellerUserId);
            return Result.Failure("User.NotFound", $"执行取消操作的用户 {request.CancellerUserId} 不存在。");
        }


        // Check if the canceller has permission
        bool canCancel = false;
        if (invitation.InviterId == request.CancellerUserId)
        {
            canCancel = true;
        }
        else
        {
            // Check if the canceller is an owner or admin of the group
            // invitation.Group should already be loaded.
            if (invitation.Group.OwnerId == request.CancellerUserId)
            {
                canCancel = true;
            }
            else
            {
                var cancellerMembership = await _groupMemberRepository.GetMemberOrDefaultAsync(invitation.GroupId, request.CancellerUserId);
                if (cancellerMembership != null && 
                    (cancellerMembership.Role == GroupMemberRole.Admin || cancellerMembership.Role == GroupMemberRole.Owner))
                {
                    canCancel = true;
                }
            }
        }

        if (!canCancel)
        {
            _logger.LogWarning("Cancel invitation failed: User {CancellerUserId} does not have permission to cancel invitation {InvitationId}. Inviter is {InviterId}.",
                request.CancellerUserId, request.InvitationId, invitation.InviterId);
            return Result.Failure("GroupInvitation.Cancel.AccessDenied", "您无权取消此邀请。");
        }

        if (invitation.Status != GroupInvitationStatus.Pending)
        {
            _logger.LogWarning("Cancel invitation failed: Invitation {InvitationId} is not pending. Current status: {Status}",
                request.InvitationId, invitation.Status);
            return Result.Failure("GroupInvitation.NotPending", $"此邀请已处理或已过期，无法取消 (状态: {invitation.Status})。");
        }
        
        if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
        {
            invitation.UpdateStatus(GroupInvitationStatus.Expired, request.CancellerUserId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Attempt to cancel invitation {InvitationId} which has already expired at {ExpiresAt}. Status updated to Expired.",
                request.InvitationId, invitation.ExpiresAt.Value);
            return Result.Failure("GroupInvitation.Expired", "此邀请已过期，无法取消。");
        }

        invitation.UpdateStatus(GroupInvitationStatus.Cancelled, request.CancellerUserId);

        // Add domain event
        var cancelledEvent = new GroupInvitationCancelledEvent(
            invitationId: invitation.Id,
            groupId: invitation.GroupId,
            groupName: invitation.Group.Name,
            invitedUserId: invitation.InvitedUserId,
            invitedUsername: invitation.InvitedUser.Username,
            cancellerUserId: request.CancellerUserId,
            cancellerUsername: cancellerUser.Username
        );
        invitation.AddDomainEvent(cancelledEvent);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {CancellerUserId} successfully cancelled invitation {InvitationId} for group {GroupId}",
            request.CancellerUserId, invitation.Id, invitation.GroupId);

        return Result.Success();
    }
}