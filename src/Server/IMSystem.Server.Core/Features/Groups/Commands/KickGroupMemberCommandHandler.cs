using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; 
using IMSystem.Server.Domain.Enums;   
using IMSystem.Server.Domain.Events.Groups; // Added for GroupMemberKickedEvent
using MediatR;
using Microsoft.Extensions.Logging;

namespace IMSystem.Server.Core.Features.Groups.Commands;

public class KickGroupMemberCommandHandler : IRequestHandler<KickGroupMemberCommand, Result>
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUserRepository _userRepository; // Added to get usernames
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<KickGroupMemberCommandHandler> _logger;

    public KickGroupMemberCommandHandler(
        IGroupRepository groupRepository,
        IGroupMemberRepository groupMemberRepository,
        IUserRepository userRepository, // Added
        IUnitOfWork unitOfWork,
        ILogger<KickGroupMemberCommandHandler> logger)
    {
        _groupRepository = groupRepository;
        _groupMemberRepository = groupMemberRepository;
        _userRepository = userRepository; // Added
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(KickGroupMemberCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Actor {ActorUserId} attempting to kick member {MemberUserIdToKick} from group {GroupId}",
            request.ActorUserId, request.MemberUserIdToKick, request.GroupId);

        var group = await _groupRepository.GetByIdAsync(request.GroupId);
        if (group == null)
        {
            _logger.LogWarning("Kick member failed: Group {GroupId} not found.", request.GroupId);
            return Result.Failure("Group.NotFound", $"群组 {request.GroupId} 不存在。");
        }

        var actorMembership = await _groupMemberRepository.GetMemberOrDefaultAsync(request.GroupId, request.ActorUserId);
        if (actorMembership == null)
        {
            _logger.LogWarning("Kick member failed: Actor {ActorUserId} is not a member of group {GroupId}.", request.ActorUserId, request.GroupId);
            return Result.Failure("Group.Kick.ActorNotMember", "操作者不是群组成员，无权执行此操作。");
        }

        var memberToKickMembership = await _groupMemberRepository.GetMemberOrDefaultAsync(request.GroupId, request.MemberUserIdToKick);
        if (memberToKickMembership == null)
        {
            _logger.LogWarning("Kick member failed: User {MemberUserIdToKick} is not a member of group {GroupId}.", request.MemberUserIdToKick, request.GroupId);
            return Result.Failure("Group.Kick.TargetNotMember", $"用户 {request.MemberUserIdToKick} 不是群组 {request.GroupId} 的成员。");
        }

        // Fetch user details for event
        var actorUser = await _userRepository.GetByIdAsync(request.ActorUserId);
        var kickedUser = await _userRepository.GetByIdAsync(request.MemberUserIdToKick);

        if (actorUser == null || kickedUser == null)
        {
            _logger.LogError("Kick member failed: Could not retrieve user details for actor {ActorUserId} or kicked user {KickedUserId}.", request.ActorUserId, request.MemberUserIdToKick);
            return Result.Failure("User.NotFound", "无法获取用户信息以完成操作。");
        }

        // Permission checks
        if (request.ActorUserId == request.MemberUserIdToKick)
        {
            _logger.LogWarning("Kick member failed: Actor {ActorUserId} cannot kick themselves from group {GroupId}.", request.ActorUserId, request.GroupId);
            return Result.Failure("Group.Kick.CannotKickSelf", "您不能将自己踢出群组。请使用退群功能。");
        }

        if (memberToKickMembership.UserId == group.OwnerId)
        {
            _logger.LogWarning("Kick member failed: Actor {ActorUserId} cannot kick group owner {MemberUserIdToKick} from group {GroupId}.",
                request.ActorUserId, request.MemberUserIdToKick, request.GroupId);
            return Result.Failure("Group.Kick.CannotKickOwner", "不能将群主踢出群组。");
        }

        bool hasPermissionToKick = false;
        if (actorMembership.Role == GroupMemberRole.Owner)
        {
            if (memberToKickMembership.Role == GroupMemberRole.Admin || memberToKickMembership.Role == GroupMemberRole.Member)
            {
                hasPermissionToKick = true;
            }
        }
        else if (actorMembership.Role == GroupMemberRole.Admin)
        {
            if (memberToKickMembership.Role == GroupMemberRole.Member)
            {
                hasPermissionToKick = true;
            }
        }

        if (!hasPermissionToKick)
        {
            _logger.LogWarning("Kick member failed: Actor {ActorUserId} (Role: {ActorRole}) does not have permission to kick member {MemberUserIdToKick} (Role: {MemberRole}) from group {GroupId}.",
                request.ActorUserId, actorMembership.Role, request.MemberUserIdToKick, memberToKickMembership.Role, request.GroupId);
            return Result.Failure("Group.Kick.PermissionDenied", "您的权限不足以踢出该成员。");
        }

        _groupMemberRepository.Remove(memberToKickMembership);
        
        // Add domain event to the group entity
        var kickedEvent = new GroupMemberKickedEvent(
            groupId: group.Id,
            groupName: group.Name,
            kickedUserId: kickedUser.Id,
            kickedUsername: kickedUser.Username,
            actorUserId: actorUser.Id,
            actorUsername: actorUser.Username
        );
        group.AddDomainEvent(kickedEvent);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Actor {ActorUserId} successfully kicked member {MemberUserIdToKick} from group {GroupId}",
            request.ActorUserId, request.MemberUserIdToKick, request.GroupId);

        return Result.Success();
    }
}