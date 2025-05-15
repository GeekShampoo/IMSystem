using IMSystem.Protocol.Common; // For Result
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For UserFriendGroup entity etc.
using IMSystem.Server.Domain.Enums; // For FriendshipStatus
using IMSystem.Server.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using IMSystem.Server.Domain.Events.FriendGroups;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

public class AddFriendToGroupCommandHandler : IRequestHandler<AddFriendToGroupCommand, Result>
{
    private readonly IFriendGroupRepository _friendGroupRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUserFriendGroupRepository _userFriendGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddFriendToGroupCommandHandler> _logger;

    public AddFriendToGroupCommandHandler(
        IFriendGroupRepository friendGroupRepository,
        IFriendshipRepository friendshipRepository,
        IUserFriendGroupRepository userFriendGroupRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddFriendToGroupCommandHandler> logger)
    {
        _friendGroupRepository = friendGroupRepository;
        _friendshipRepository = friendshipRepository;
        _userFriendGroupRepository = userFriendGroupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AddFriendToGroupCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("用户 {CurrentUserId} 尝试将好友 (FriendshipId: {FriendshipId}) 添加到分组 {GroupId}。",
            request.CurrentUserId, request.FriendshipId, request.GroupId);

        // 1. 验证目标分组是否存在且属于当前用户
        var friendGroup = await _friendGroupRepository.GetByIdAsync(request.GroupId);
        if (friendGroup == null)
        {
            _logger.LogWarning("添加好友到分组失败：未找到ID为 {GroupId} 的分组。", request.GroupId);
            return Result.Failure("FriendGroup.NotFound", $"未找到ID为 {request.GroupId} 的分组。");
        }
        if (friendGroup.CreatedBy != request.CurrentUserId)
        {
            _logger.LogWarning("添加好友到分组失败：用户 {CurrentUserId} 无权操作不属于自己的分组 {GroupId} (拥有者: {OwnerId})。",
                request.CurrentUserId, request.GroupId, friendGroup.CreatedBy);
            return Result.Failure("FriendGroup.AccessDenied", "您无权操作此好友分组。");
        }

        // 2. 验证好友关系是否存在，是否已接受，以及是否与当前用户相关
        var friendship = await _friendshipRepository.GetByIdAsync(request.FriendshipId);
        if (friendship == null)
        {
            _logger.LogWarning("添加好友到分组失败：未找到ID为 {FriendshipId} 的好友关系。", request.FriendshipId);
            return Result.Failure("Friendship.NotFound", $"未找到ID为 {request.FriendshipId} 的好友关系。");
        }
        if (friendship.Status != FriendshipStatus.Accepted)
        {
            _logger.LogWarning("添加好友到分组失败：好友关系 {FriendshipId} 尚未被接受 (状态: {Status})。",
                request.FriendshipId, friendship.Status);
            return Result.Failure("Friendship.NotAccepted", "只有已接受的好友才能添加到分组。");
        }
        // 确保当前用户是好友关系的一方
        if (friendship.RequesterId != request.CurrentUserId && friendship.AddresseeId != request.CurrentUserId)
        {
            _logger.LogWarning("添加好友到分组失败：用户 {CurrentUserId} 不是好友关系 {FriendshipId} 的参与方。",
                request.CurrentUserId, request.FriendshipId);
            return Result.Failure("Friendship.AccessDenied", "您无权操作此好友关系。");
        }

        // 3. 检查好友是否已在该分组中
        var existingAssignmentInTargetGroup = await _userFriendGroupRepository.GetByFriendGroupIdAndFriendshipIdAsync(request.GroupId, request.FriendshipId);
        if (existingAssignmentInTargetGroup != null)
        {
            _logger.LogInformation("好友 (FriendshipId: {FriendshipId}) 已在目标分组 {GroupId} 中，无需操作。",
                request.FriendshipId, request.GroupId);
            return Result.Success(); // 已在目标分组，视为成功
        }

        // 4. 检查好友是否已在当前用户的其他分组中 (因为一个好友在一个用户下只能属于一个分组)
        // UserFriendGroup.CreatedBy 存储的是分组拥有者的ID，即 request.CurrentUserId
        var existingAssignmentForFriend = await _userFriendGroupRepository.GetByUserIdAndFriendshipIdAsync(request.CurrentUserId, request.FriendshipId);
        if (existingAssignmentForFriend != null)
        {
            // 好友已在其他分组，先移除旧的关联
            _logger.LogInformation("好友 (FriendshipId: {FriendshipId}) 当前在分组 {OldGroupId}，将从旧分组移除。",
                request.FriendshipId, existingAssignmentForFriend.FriendGroupId);
            _userFriendGroupRepository.Remove(existingAssignmentForFriend);
        }

        // 5. 创建新的 UserFriendGroup 关联
        var newUserFriendGroup = new UserFriendGroup(request.CurrentUserId, request.FriendshipId, request.GroupId);
        await _userFriendGroupRepository.AddAsync(newUserFriendGroup);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("用户 {CurrentUserId} 成功将好友 (FriendshipId: {FriendshipId}) 添加到分组 {GroupId}。",
            request.CurrentUserId, request.FriendshipId, request.GroupId);
        
        // 触发领域事件 FriendAddedToGroupEvent
        newUserFriendGroup.AddDomainEvent(
            new FriendAddedToGroupEvent(
                newUserFriendGroup.Id,
                newUserFriendGroup.UserId,
                newUserFriendGroup.FriendshipId
            )
        );

        return Result.Success();
    }
}