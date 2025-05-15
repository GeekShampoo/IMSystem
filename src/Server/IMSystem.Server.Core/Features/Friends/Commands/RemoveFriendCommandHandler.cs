using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Protocol.Common;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Exceptions;
using IMSystem.Server.Domain.Events; // Added for FriendRemovedEvent

using IMSystem.Server.Core.Common;
using IMSystem.Server.Domain.Events.Friends;

namespace IMSystem.Server.Core.Features.Friends.Commands;

/// <summary>
/// 处理 <see cref="RemoveFriendCommand"/> 的处理器。
/// </summary>
public class RemoveFriendCommandHandler : IRequestHandler<RemoveFriendCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUserFriendGroupRepository _userFriendGroupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFriendCommandHandler(
        IUserRepository userRepository,
        IFriendshipRepository friendshipRepository,
        IUserFriendGroupRepository userFriendGroupRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _friendshipRepository = friendshipRepository ?? throw new ArgumentNullException(nameof(friendshipRepository));
        _userFriendGroupRepository = userFriendGroupRepository ?? throw new ArgumentNullException(nameof(userFriendGroupRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> Handle(RemoveFriendCommand request, CancellationToken cancellationToken)
    {
        // 1. 验证当前用户和目标好友是否存在
        var currentUser = await _userRepository.GetByIdAsync(request.CurrentUserId);
        if (currentUser is null)
        {
            return Result.Failure(UserErrors.NotFound, UserErrors.UserNotFoundDescription(request.CurrentUserId));
        }

        var friendUser = await _userRepository.GetByIdAsync(request.FriendUserId);
        if (friendUser is null)
        {
            return Result.Failure(UserErrors.NotFound, UserErrors.UserNotFoundDescription(request.FriendUserId));
        }

        // 2. 查找双方的好友关系记录
        var friendship = await _friendshipRepository.GetFriendshipBetweenUsersAsync(request.CurrentUserId, request.FriendUserId);
        if (friendship is null)
        {
            // 如果没有好友关系，可以认为操作成功（幂等性）或返回特定错误
            return Result.Failure(FriendshipErrors.NotFound, FriendshipErrors.NotFriendsDescription(request.CurrentUserId, request.FriendUserId));
        }

        // 3. 移除好友关系
        _friendshipRepository.Remove(friendship);

        // 4. 移除双方在 UserFriendGroup 中的关联记录
        // 这个操作会移除所有与该 FriendshipId 相关的 UserFriendGroup 条目
        await _userFriendGroupRepository.RemoveByFriendshipIdAsync(friendship.Id, cancellationToken);

        // 5. 添加领域事件
        // currentUser is already fetched and contains the username.
        // friendship entity should have AddDomainEvent if it inherits from a base entity that supports domain events.
        friendship.AddDomainEvent(new FriendRemovedEvent(friendship.Id, request.CurrentUserId, request.FriendUserId, currentUser.Username));
        
        // 6. 保存更改 (这将持久化删除操作并发布事件)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
