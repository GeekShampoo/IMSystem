using IMSystem.Protocol.Common; // For Result
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // May be needed for other domain entities if used
using IMSystem.Server.Domain.Enums;   // For FriendshipStatus
using IMSystem.Server.Domain.Exceptions; // For EntityNotFoundException
using IMSystem.Server.Domain.Events; // Added for FriendRequestDeclinedEvent
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Events.Friends;

namespace IMSystem.Server.Core.Features.Friends.Commands;

public class DeclineFriendRequestCommandHandler : IRequestHandler<DeclineFriendRequestCommand, Result>
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUserRepository _userRepository; // Added to get decliner's details
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeclineFriendRequestCommandHandler> _logger;

    public DeclineFriendRequestCommandHandler(
        IFriendshipRepository friendshipRepository,
        IUserRepository userRepository, // Added
        IUnitOfWork unitOfWork,
        ILogger<DeclineFriendRequestCommandHandler> logger)
    {
        _friendshipRepository = friendshipRepository;
        _userRepository = userRepository; // Added
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeclineFriendRequestCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("用户 {CurrentUserId} 尝试拒绝好友请求 {FriendshipId}。", request.CurrentUserId, request.FriendshipId);

        var friendship = await _friendshipRepository.GetByIdAsync(request.FriendshipId);

        if (friendship == null)
        {
            _logger.LogWarning("拒绝好友请求失败：未找到ID为 {FriendshipId} 的好友请求。", request.FriendshipId);
            return Result.Failure("FriendRequest.NotFound", $"未找到ID为 {request.FriendshipId} 的好友请求。");
        }

        if (friendship.AddresseeId != request.CurrentUserId)
        {
            _logger.LogWarning("拒绝好友请求失败：用户 {CurrentUserId} 不是好友请求 {FriendshipId} 的接收者 (接收者为 {AddresseeId})。",
                request.CurrentUserId, request.FriendshipId, friendship.AddresseeId);
            return Result.Failure("FriendRequest.AccessDenied", "您无权拒绝此好友请求。");
        }

        if (friendship.Status != FriendshipStatus.Pending)
        {
            _logger.LogWarning("拒绝好友请求失败：好友请求 {FriendshipId} 的状态为 {Status}，不是 Pending。", request.FriendshipId, friendship.Status);
            return Result.Failure("FriendRequest.NotPending", $"无法拒绝好友请求，当前状态为：{friendship.Status}。");
        }

        // 检查好友请求是否已过期
        if (friendship.RequestExpiresAt.HasValue && friendship.RequestExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("拒绝好友请求失败：好友请求 {FriendshipId} 已过期。过期时间：{ExpiresAt}，当前时间：{Now}",
                request.FriendshipId, friendship.RequestExpiresAt.Value, DateTimeOffset.UtcNow);
            return Result.Failure("FriendRequest.Expired", "此好友请求已过期，无法拒绝。");
        }

        try
        {
            friendship.DeclineRequest(request.CurrentUserId);

            // 获取拒绝者信息以用于事件
            var decliner = await _userRepository.GetByIdAsync(request.CurrentUserId);
            if (decliner == null)
            {
                 _logger.LogError("无法找到拒绝者用户 {CurrentUserId} 的信息，无法触发 FriendRequestDeclinedEvent。", request.CurrentUserId);
                // Decide on error handling. For now, log and proceed.
            }
            else
            {
                friendship.AddDomainEvent(new FriendRequestDeclinedEvent(friendship.Id, friendship.RequesterId, friendship.AddresseeId, decliner.Username));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken); // Saves Friendship and queues the event
            _logger.LogInformation("已成功拒绝好友请求 {FriendshipId} 并将事件加入队列。", request.FriendshipId);
            
            return Result.Success();
        }
        catch (InvalidOperationException ex) // 来自 friendship.DeclineRequest 的内部验证
        {
            _logger.LogWarning("拒绝好友请求 {FriendshipId} 操作失败: {ErrorMessage}", request.FriendshipId, ex.Message);
            return Result.Failure("FriendRequest.InvalidOperation", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "拒绝好友请求 {FriendshipId} 过程中发生意外错误。", request.FriendshipId);
            return Result.Failure("FriendRequest.UnexpectedError", $"拒绝好友请求时发生内部错误: {ex.Message}");
        }
    }
}