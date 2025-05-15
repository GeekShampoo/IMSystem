using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Enums; // Added for GroupInvitationStatus
using IMSystem.Server.Domain.Events.Groups; // For GroupInvitationSentEvent
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class InviteUserToGroupCommandHandler : IRequestHandler<InviteUserToGroupCommand, Result<Guid>>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IGroupInvitationRepository _groupInvitationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InviteUserToGroupCommandHandler> _logger;

    public InviteUserToGroupCommandHandler(
        IGroupRepository groupRepository,
        IUserRepository userRepository,
        IGroupMemberRepository groupMemberRepository,
        IGroupInvitationRepository groupInvitationRepository,
        IUnitOfWork unitOfWork,
        ILogger<InviteUserToGroupCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _groupMemberRepository = groupMemberRepository;
        _groupInvitationRepository = groupInvitationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(InviteUserToGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("用户 {InviterUserId} 尝试邀请用户 {InvitedUserId} 加入群组 {GroupId}。",
            request.InviterUserId, request.InvitedUserId, request.GroupId);

        // 1. 验证群组是否存在
        var group = await _groupRepository.GetByIdAsync(request.GroupId);
        if (group == null)
        {
            _logger.LogWarning("邀请用户失败：未找到群组 {GroupId}。", request.GroupId);
            return Result<Guid>.Failure("Group.NotFound", $"群组 {request.GroupId} 不存在。");
        }

        // 2. 验证被邀请用户是否存在
        var invitedUser = await _userRepository.GetByIdAsync(request.InvitedUserId);
        if (invitedUser == null)
        {
            _logger.LogWarning("邀请用户失败：未找到被邀请用户 {InvitedUserId}。", request.InvitedUserId);
            return Result<Guid>.Failure("User.NotFound.Invited", $"用户 {request.InvitedUserId} 不存在。");
        }

        // 3. 验证邀请者是否存在
        var inviterUser = await _userRepository.GetByIdAsync(request.InviterUserId);
        if (inviterUser == null)
        {
            _logger.LogWarning("邀请用户失败：未找到邀请者 {InviterUserId}。", request.InviterUserId);
            return Result<Guid>.Failure("User.NotFound.Inviter", $"用户 {request.InviterUserId} 不存在。");
        }

        // 4. 验证邀请者是否有权邀请
        var inviterMembership = await _groupMemberRepository.GetMemberOrDefaultAsync(request.GroupId, request.InviterUserId);
        if (inviterMembership == null && group.OwnerId != request.InviterUserId) // 群主总是有权邀请
        {
            _logger.LogWarning("邀请用户失败：用户 {InviterUserId} 不是群组 {GroupId} 的成员，无权邀请。",
                request.InviterUserId, request.GroupId);
            return Result<Guid>.Failure("Group.Invite.AccessDenied", "您不是该群组成员，无权邀请。");
        }
        // TODO: 可以进一步检查邀请者的角色 (e.g., GroupMemberRole.Admin) 是否有邀请权限

        // 5. 验证被邀请用户是否已经是群成员
        var existingMembership = await _groupMemberRepository.GetMemberOrDefaultAsync(request.GroupId, request.InvitedUserId);
        if (existingMembership != null)
        {
            _logger.LogInformation("用户 {InvitedUserId} 已经是群组 {GroupId} 的成员。", request.InvitedUserId, request.GroupId);
            return Result<Guid>.Failure("Group.Invite.AlreadyMember", $"用户 {invitedUser.Username} 已经是该群组的成员。");
        }

        // 6. 检查是否已存在待处理的邀请
        var existingInvitation = await _groupInvitationRepository.FindByGroupAndInvitedUserAsync(request.GroupId, request.InvitedUserId);
        if (existingInvitation != null)
        {
            _logger.LogInformation("用户 {InvitedUserId} 已有待处理的群组 {GroupId} 邀请。", request.InvitedUserId, request.GroupId);
            return Result<Guid>.Failure("Group.Invite.PendingExists", $"已向用户 {invitedUser.Username} 发送过该群组的邀请，请等待对方处理。");
        }

        // 7. 创建群组邀请
        var groupInvitation = new GroupInvitation(
            groupId: request.GroupId,
            inviterId: request.InviterUserId,
            invitedUserId: request.InvitedUserId,
            message: request.Message,
            expiresAt: request.ExpiresAt
        );

        await _groupInvitationRepository.AddAsync(groupInvitation);

        // 8. 添加领域事件
        var invitationSentEvent = new GroupInvitationSentEvent(
            invitationId: groupInvitation.Id,
            groupId: group.Id,
            groupName: group.Name, // Assuming Group entity has a Name property
            inviterUserId: inviterUser.Id,
            inviterUsername: inviterUser.Username, // Assuming User entity has a Username property
            invitedUserId: invitedUser.Id,
            invitedUsername: invitedUser.Username, // Assuming User entity has a Username property
            message: groupInvitation.Message,
            expiresAt: groupInvitation.ExpiresAt
        );
        groupInvitation.AddDomainEvent(invitationSentEvent);
        
        // 9. 保存更改并发布事件 (通过Outbox)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("成功为用户 {InvitedUserId} 创建了群组 {GroupId} 的邀请，邀请ID: {InvitationId}。",
            request.InvitedUserId, request.GroupId, groupInvitation.Id);

        return Result<Guid>.Success(groupInvitation.Id);
    }
}