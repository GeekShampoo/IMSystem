using IMSystem.Protocol.Common; // For Result
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For UserFriendGroup etc.
using IMSystem.Server.Domain.Enums;   // For FriendshipStatus
using IMSystem.Server.Domain.Exceptions; // For EntityNotFoundException
using IMSystem.Server.Domain.Events; // Added for FriendRequestAcceptedEvent
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Events.Friends;

namespace IMSystem.Server.Core.Features.Friends.Commands;

public class AcceptFriendRequestCommandHandler : IRequestHandler<AcceptFriendRequestCommand, Result>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IFriendGroupRepository _friendGroupRepository;
    private readonly IUserRepository _userRepository; // Added to get accepter's details
    private readonly IUserFriendGroupRepository _userFriendGroupRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptFriendRequestCommandHandler> _logger;

    public AcceptFriendRequestCommandHandler(
        IFriendshipRepository friendshipRepository,
        IFriendGroupRepository friendGroupRepository,
        IUserRepository userRepository, // Added
        IUserFriendGroupRepository userFriendGroupRepository,
        IUnitOfWork unitOfWork,
        ILogger<AcceptFriendRequestCommandHandler> logger)
    {
        _friendshipRepository = friendshipRepository;
        _friendGroupRepository = friendGroupRepository;
        _userRepository = userRepository; // Added
        _userFriendGroupRepository = userFriendGroupRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AcceptFriendRequestCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("用户 {CurrentUserId} 尝试接受好友请求 {FriendshipId}。", request.CurrentUserId, request.FriendshipId);

        var friendship = await _friendshipRepository.GetByIdAsync(request.FriendshipId);

        if (friendship == null)
        {
            _logger.LogWarning("接受好友请求失败：未找到ID为 {FriendshipId} 的好友请求。", request.FriendshipId);
            return Result.Failure("FriendRequest.NotFound", $"未找到ID为 {request.FriendshipId} 的好友请求。");
        }

        if (friendship.AddresseeId != request.CurrentUserId)
        {
            _logger.LogWarning("接受好友请求失败：用户 {CurrentUserId} 不是好友请求 {FriendshipId} 的接收者 (接收者为 {AddresseeId})。",
                request.CurrentUserId, request.FriendshipId, friendship.AddresseeId);
            return Result.Failure("FriendRequest.AccessDenied", "您无权接受此好友请求。");
        }

        if (friendship.Status != FriendshipStatus.Pending)
        {
            _logger.LogWarning("接受好友请求失败：好友请求 {FriendshipId} 的状态为 {Status}，不是 Pending。", request.FriendshipId, friendship.Status);
            return Result.Failure("FriendRequest.NotPending", $"无法接受好友请求，当前状态为：{friendship.Status}。");
        }

        // 检查好友请求是否已过期
        if (friendship.RequestExpiresAt.HasValue && friendship.RequestExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("接受好友请求失败：好友请求 {FriendshipId} 已过期。过期时间：{ExpiresAt}，当前时间：{Now}",
                request.FriendshipId, friendship.RequestExpiresAt.Value, DateTimeOffset.UtcNow);
            return Result.Failure("FriendRequest.Expired", "此好友请求已过期，无法接受。");
        }

        try
        {
            friendship.AcceptRequest(request.CurrentUserId);

            _logger.LogInformation("用户 {CurrentUserId} 成功接受了来自用户 {RequesterId} 的好友请求 {FriendshipId}。现在尝试将双方添加到各自的默认分组。",
                request.CurrentUserId, friendship.RequesterId, request.FriendshipId);

            // 将双方添加到各自的默认分组
            await AddFriendToUserDefaultGroupAsync(friendship.RequesterId, friendship.Id, cancellationToken);
            await AddFriendToUserDefaultGroupAsync(friendship.AddresseeId, friendship.Id, cancellationToken);

            // 获取接受者信息以用于事件
            var accepter = await _userRepository.GetByIdAsync(request.CurrentUserId);
            if (accepter == null)
            {
                _logger.LogError("无法找到接受者用户 {CurrentUserId} 的信息，无法触发 FriendRequestAcceptedEvent。", request.CurrentUserId);
            }
            else
            {
                friendship.AddDomainEvent(new FriendRequestAcceptedEvent(friendship.Id, friendship.RequesterId, friendship.AddresseeId, accepter.Username, accepter.Profile?.Nickname, accepter.Profile?.AvatarUrl));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken); // Saves Friendship, UserFriendGroups, and queues the event
            _logger.LogInformation("已成功接受好友请求 {FriendshipId} 并将事件加入队列。", request.FriendshipId);

            return Result.Success();
        }
        catch (InvalidOperationException ex) // 来自 friendship.AcceptRequest 的内部验证
        {
            _logger.LogWarning("接受好友请求 {FriendshipId} 操作失败: {ErrorMessage}", request.FriendshipId, ex.Message);
            return Result.Failure("FriendRequest.InvalidOperation", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "接受好友请求 {FriendshipId} 过程中发生意外错误。", request.FriendshipId);
            return Result.Failure("FriendRequest.UnexpectedError", $"接受好友请求时发生内部错误: {ex.Message}");
        }
    }

    private async Task AddFriendToUserDefaultGroupAsync(Guid userId, Guid friendshipId, CancellationToken cancellationToken)
    {
        var defaultGroup = await _friendGroupRepository.GetDefaultByUserIdAsync(userId);
        if (defaultGroup == null)
        {
            _logger.LogError("用户 {UserId} 没有默认好友分组。无法自动添加好友 (FriendshipId: {FriendshipId}) 到默认分组。",
                userId, friendshipId);
            return; 
        }

        var existingAssignment = await _userFriendGroupRepository.GetByFriendGroupIdAndFriendshipIdAsync(defaultGroup.Id, friendshipId);
        if (existingAssignment == null)
        {
            var newUserFriendGroup = new UserFriendGroup(userId, friendshipId, defaultGroup.Id);
            await _userFriendGroupRepository.AddAsync(newUserFriendGroup);
            _logger.LogInformation("已准备将好友 (FriendshipId: {FriendshipId}) 添加到用户 {UserId} 的默认分组 {DefaultGroupId} (名称: '{DefaultGroupName}')。",
                friendshipId, userId, defaultGroup.Id, defaultGroup.Name);
        }
        else
        {
            _logger.LogInformation("好友 (FriendshipId: {FriendshipId}) 已存在于用户 {UserId} 的默认分组 {DefaultGroupId}。",
                friendshipId, userId, defaultGroup.Id);
        }
    }
}