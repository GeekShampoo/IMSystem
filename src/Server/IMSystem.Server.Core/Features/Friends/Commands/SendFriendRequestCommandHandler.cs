using IMSystem.Protocol.Common; // For Result
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For Friendship, User entities
using IMSystem.Server.Domain.Enums;   // For FriendshipStatus
using MediatR;
using IMSystem.Server.Domain.Events; // Added for FriendRequestSentEvent
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Events.Friends;

namespace IMSystem.Server.Core.Features.Friends.Commands;

public class SendFriendRequestCommandHandler : IRequestHandler<SendFriendRequestCommand, Result<Guid>>
{
    private readonly IUserRepository _userRepository;
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendFriendRequestCommandHandler> _logger;

    public SendFriendRequestCommandHandler(
        IUserRepository userRepository,
        IFriendshipRepository friendshipRepository,
        IUnitOfWork unitOfWork,
        ILogger<SendFriendRequestCommandHandler> logger)
    {
        _userRepository = userRepository;
        _friendshipRepository = friendshipRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(SendFriendRequestCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("用户 {RequesterId} 尝试向用户 {AddresseeId} 发送好友请求。", request.RequesterId, request.AddresseeId);

        // 1. 验证接收者是否存在
        var addressee = await _userRepository.GetByIdAsync(request.AddresseeId);
        var requester = await _userRepository.GetByIdAsync(request.RequesterId); // Get requester info

        if (addressee == null || requester == null)
        {
            if (addressee == null)
                _logger.LogWarning("发送好友请求失败：接收者用户 {AddresseeId} 不存在。", request.AddresseeId);
            if (requester == null)
                _logger.LogWarning("发送好友请求失败：请求者用户 {RequesterId} 不存在。", request.RequesterId);
            
            return Result<Guid>.Failure("User.NotFound", addressee == null
                ? $"目标用户 (ID: {request.AddresseeId}) 不存在。"
                : $"请求用户 (ID: {request.RequesterId}) 不存在。");
        }

        // 2. 检查是否已存在好友关系或待处理请求
        var existingFriendship = await _friendshipRepository.GetFriendshipBetweenUsersAsync(request.RequesterId, request.AddresseeId);
        if (existingFriendship != null)
        {
            string message;
            string errorCode = "FriendRequest.Conflict"; // Default error code for conflicts
            switch (existingFriendship.Status)
            {
                case FriendshipStatus.Pending:
                    if (existingFriendship.RequesterId == request.RequesterId && existingFriendship.AddresseeId == request.AddresseeId)
                    {
                        message = "您已向该用户发送过好友请求，请等待对方处理。";
                        errorCode = "FriendRequest.Pending.Self";
                    }
                    else
                    {
                        message = "对方已向您发送好友请求，请前往处理。";
                        errorCode = "FriendRequest.Pending.Other";
                    }
                    break;
                case FriendshipStatus.Accepted:
                    message = "您们已经是好友了。";
                    errorCode = "FriendRequest.AlreadyFriends";
                    break;
                case FriendshipStatus.Declined:
                    if (existingFriendship.LastModifiedBy == request.AddresseeId)
                    {
                        message = "您之前发送的好友请求已被对方拒绝。";
                        errorCode = "FriendRequest.DeclinedByOther";
                    }
                    else
                    {
                         message = "您之前已拒绝过对方的好友请求。";
                         errorCode = "FriendRequest.DeclinedBySelf";
                    }
                    break;
                case FriendshipStatus.Blocked:
                    message = "无法发送好友请求，可能存在阻止关系。";
                    errorCode = "FriendRequest.Blocked";
                    break;
                default:
                    message = "已存在一个好友关系记录，无法重复发送请求。";
                    break;
            }
            _logger.LogWarning("发送好友请求失败：{Reason} (Requester: {RequesterId}, Addressee: {AddresseeId})", message, request.RequesterId, request.AddresseeId);
            return Result<Guid>.Failure(errorCode, message);
        }

        // 3. 创建并保存好友请求
        var newFriendship = new Friendship(request.RequesterId, request.AddresseeId);
        // Set remarks if provided
        if (!string.IsNullOrWhiteSpace(request.RequesterRemark))
        {
            newFriendship.UpdateRemark(request.RequesterId, request.RequesterRemark);
        }

        // 触发领域事件
        newFriendship.AddDomainEvent(new FriendRequestSentEvent(
            newFriendship.Id,
            requester.Id,
            addressee.Id,
            requester.Username,
            requester.Profile?.Nickname,
            requester.Profile?.AvatarUrl));

        await _friendshipRepository.AddAsync(newFriendship);
        await _unitOfWork.SaveChangesAsync(cancellationToken); // 这会将事件保存到 Outbox

        _logger.LogInformation("用户 {RequesterId} 成功向用户 {AddresseeId} 发送好友请求。好友关系ID: {FriendshipId}", request.RequesterId, request.AddresseeId, newFriendship.Id);
        return Result<Guid>.Success(newFriendship.Id);
    }
}