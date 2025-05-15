using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Enums;
using IMSystem.Server.Domain.Events.Groups; // Added for GroupInvitationRejectedEvent
using IMSystem.Server.Domain.Entities; // Required for GroupInvitation entity access for Group and InvitedUser
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class RejectGroupInvitationCommandHandler : IRequestHandler<RejectGroupInvitationCommand, Result>
{
    private readonly IGroupInvitationRepository _groupInvitationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectGroupInvitationCommandHandler> _logger;
    // It seems we might need IUserRepository if we want to get the username of the rejector,
    // or ensure invitation.InvitedUser is loaded.
    // For now, assuming invitation.InvitedUser and invitation.Group are loaded by GetByIdAsync.

    public RejectGroupInvitationCommandHandler(
        IGroupInvitationRepository groupInvitationRepository,
        IUnitOfWork unitOfWork,
        ILogger<RejectGroupInvitationCommandHandler> logger)
    {
        _groupInvitationRepository = groupInvitationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(RejectGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} attempting to reject group invitation {InvitationId}", request.UserId, request.InvitationId);

        var invitation = await _groupInvitationRepository.GetByIdAsync(request.InvitationId); // Assumes Group and InvitedUser are included

        if (invitation == null)
        {
            _logger.LogWarning("Reject invitation failed: Invitation {InvitationId} not found.", request.InvitationId);
            return Result.Failure("GroupInvitation.NotFound", $"邀请 {request.InvitationId} 不存在。");
        }

        if (invitation.InvitedUserId != request.UserId)
        {
            _logger.LogWarning("Reject invitation failed: User {UserId} is not the invited user for invitation {InvitationId}. Invited user is {InvitedUserId}",
                request.UserId, request.InvitationId, invitation.InvitedUserId);
            return Result.Failure("GroupInvitation.AccessDenied", "您无权操作此邀请。");
        }

        if (invitation.Status != GroupInvitationStatus.Pending)
        {
            _logger.LogWarning("Reject invitation failed: Invitation {InvitationId} is not pending. Current status: {Status}",
                request.InvitationId, invitation.Status);
            return Result.Failure("GroupInvitation.NotPending", $"此邀请已处理或已过期 (状态: {invitation.Status})。");
        }
        
        if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
        {
            invitation.UpdateStatus(GroupInvitationStatus.Expired, request.UserId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Reject invitation failed as it's already expired: Invitation {InvitationId} expired at {ExpiresAt}.",
                request.InvitationId, invitation.ExpiresAt.Value);
            return Result.Failure("GroupInvitation.Expired", "此邀请已过期。");
        }

        invitation.UpdateStatus(GroupInvitationStatus.Rejected, request.UserId);

        // Add domain event
        // Ensure invitation.Group and invitation.InvitedUser are loaded for GroupName and Username
        if (invitation.Group == null)
        {
             _logger.LogError("Data integrity issue: Group {GroupId} for invitation {InvitationId} not found (not included or deleted) when creating RejectedEvent.", invitation.GroupId, invitation.Id);
             // Decide if to proceed without group name or fail. For now, let's proceed but log heavily.
        }
        if (invitation.InvitedUser == null)
        {
             _logger.LogError("Data integrity issue: InvitedUser {InvitedUserId} for invitation {InvitationId} not found (not included or deleted) when creating RejectedEvent.", invitation.InvitedUserId, invitation.Id);
        }

        var rejectedEvent = new GroupInvitationRejectedEvent(
            invitationId: invitation.Id,
            groupId: invitation.GroupId,
            groupName: invitation.Group?.Name ?? "未知群组", // Fallback if group not loaded
            userId: invitation.InvitedUserId, // This is request.UserId
            username: invitation.InvitedUser?.Username ?? "未知用户", // Fallback if user not loaded
            inviterUserId: invitation.InviterId
        );
        invitation.AddDomainEvent(rejectedEvent);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} successfully rejected invitation {InvitationId} for group {GroupId}",
            request.UserId, invitation.Id, invitation.GroupId);

        return Result.Success();
    }
}