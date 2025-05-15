using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Enums;
using IMSystem.Server.Domain.Events.Groups; // Added for GroupInvitationAcceptedEvent
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class AcceptGroupInvitationCommandHandler : IRequestHandler<AcceptGroupInvitationCommand, Result>
{
    private readonly IGroupInvitationRepository _groupInvitationRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IGroupRepository _groupRepository; // To verify group exists
    private readonly IUserRepository _userRepository;   // To verify user exists
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptGroupInvitationCommandHandler> _logger;

    public AcceptGroupInvitationCommandHandler(
        IGroupInvitationRepository groupInvitationRepository,
        IGroupMemberRepository groupMemberRepository,
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<AcceptGroupInvitationCommandHandler> logger)
    {
        _groupInvitationRepository = groupInvitationRepository;
        _groupMemberRepository = groupMemberRepository;
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AcceptGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {UserId} attempting to accept group invitation {InvitationId}", request.UserId, request.InvitationId);

        var invitation = await _groupInvitationRepository.GetByIdAsync(request.InvitationId); // Assumes Group and InvitedUser are included

        if (invitation == null)
        {
            _logger.LogWarning("Accept invitation failed: Invitation {InvitationId} not found.", request.InvitationId);
            return Result.Failure("GroupInvitation.NotFound", $"邀请 {request.InvitationId} 不存在。");
        }

        if (invitation.InvitedUserId != request.UserId)
        {
            _logger.LogWarning("Accept invitation failed: User {UserId} is not the invited user for invitation {InvitationId}. Invited user is {InvitedUserId}",
                request.UserId, request.InvitationId, invitation.InvitedUserId);
            return Result.Failure("GroupInvitation.AccessDenied", "您无权操作此邀请。");
        }

        if (invitation.Status != GroupInvitationStatus.Pending)
        {
            _logger.LogWarning("Accept invitation failed: Invitation {InvitationId} is not pending. Current status: {Status}",
                request.InvitationId, invitation.Status);
            return Result.Failure("GroupInvitation.NotPending", $"此邀请已处理或已过期 (状态: {invitation.Status})。");
        }

        if (invitation.ExpiresAt.HasValue && invitation.ExpiresAt.Value < DateTime.UtcNow)
        {
            invitation.UpdateStatus(GroupInvitationStatus.Expired, request.UserId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Accept invitation failed: Invitation {InvitationId} has expired at {ExpiresAt}.",
                request.InvitationId, invitation.ExpiresAt.Value);
            return Result.Failure("GroupInvitation.Expired", "此邀请已过期。");
        }

        // Verify group and user still exist (optional, but good for data integrity)
        // invitation.Group and invitation.InvitedUser should be loaded by GetByIdAsync
        if (invitation.Group == null)
        {
            _logger.LogError("Data integrity issue: Group {GroupId} for invitation {InvitationId} not found (not included or deleted).", invitation.GroupId, invitation.Id);
            return Result.Failure("Group.NotFound", $"邀请关联的群组 {invitation.GroupId} 不存在。");
        }
        if (invitation.InvitedUser == null) // This is the user accepting the invitation
        {
             _logger.LogError("Data integrity issue: InvitedUser {InvitedUserId} for invitation {InvitationId} not found (not included or deleted).", invitation.InvitedUserId, invitation.Id);
            return Result.Failure("User.NotFound", $"邀请关联的用户 {invitation.InvitedUserId} 不存在。");
        }


        // Check if user is already a member
        var existingMembership = await _groupMemberRepository.GetMemberOrDefaultAsync(invitation.GroupId, request.UserId);
        if (existingMembership != null)
        {
            _logger.LogInformation("User {UserId} is already a member of group {GroupId}. Marking invitation {InvitationId} as accepted.",
                request.UserId, invitation.GroupId, invitation.Id);
            invitation.UpdateStatus(GroupInvitationStatus.Accepted, request.UserId); 
            // Optionally, add an event here if this scenario needs specific handling, e.g., InvitationAutoAcceptedAsAlreadyMemberEvent
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(); 
        }

        // Proceed to accept invitation and add member
        invitation.UpdateStatus(GroupInvitationStatus.Accepted, request.UserId);

        var newMember = new GroupMember(
            groupId: invitation.GroupId,
            userId: request.UserId,
            role: GroupMemberRole.Member, // Default role upon accepting an invitation
            actorId: request.UserId // User themselves performed the action of joining
        );
        await _groupMemberRepository.AddAsync(newMember);

        // Add domain event
        var acceptedEvent = new GroupInvitationAcceptedEvent(
            invitationId: invitation.Id,
            groupId: invitation.GroupId,
            groupName: invitation.Group.Name, // Assumes invitation.Group is loaded and has Name
            userId: invitation.InvitedUserId, // This is request.UserId
            username: invitation.InvitedUser.Username, // Assumes invitation.InvitedUser is loaded and has Username
            inviterUserId: invitation.InviterId
        );
        invitation.AddDomainEvent(acceptedEvent); // Add to invitation entity
        // Or, if UserJoinedGroupEvent is preferred and newMember is the primary entity for that:
        // newMember.AddDomainEvent(new UserJoinedGroupEvent(newMember.GroupId, newMember.UserId, invitation.Group.Name, invitation.InvitedUser.Username));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} successfully accepted invitation {InvitationId} and joined group {GroupId} as member {MemberId}",
            request.UserId, invitation.Id, invitation.GroupId, newMember.Id);

        return Result.Success();
    }
}