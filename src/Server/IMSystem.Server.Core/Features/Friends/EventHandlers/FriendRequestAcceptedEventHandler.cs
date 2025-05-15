using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Entities; // For Message, User entities
using IMSystem.Server.Domain.Enums;   // For MessageType, MessageRecipientType
using IMSystem.Server.Domain.Events; // Assuming FriendRequestAcceptedEvent is here
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Events.Friends;

namespace IMSystem.Server.Core.Features.Friends.EventHandlers;

/// <summary>
/// 处理 FriendRequestAcceptedEvent 事件，负责处理持久化副作用：
/// 1. 为新建立的好友关系创建系统消息
/// </summary>
public class FriendRequestAcceptedEventHandler : INotificationHandler<FriendRequestAcceptedEvent>
{
    private readonly ILogger<FriendRequestAcceptedEventHandler> _logger;
    private readonly IMessageRepository _messageRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork; // To save system messages

    public FriendRequestAcceptedEventHandler(
        ILogger<FriendRequestAcceptedEventHandler> logger,
        IMessageRepository messageRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(FriendRequestAcceptedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "持久化处理：处理 FriendRequestAcceptedEvent，创建系统消息。FriendshipId: {FriendshipId}. 请求者: {RequesterId}, 接受者: {AddresseeId}",
            notification.FriendshipId, notification.RequesterId, notification.AddresseeId);

        var requester = await _userRepository.GetByIdAsync(notification.RequesterId);
        var addressee = await _userRepository.GetByIdAsync(notification.AddresseeId);

        if (requester == null || addressee == null)
        {
            _logger.LogError("为好友请求接受事件创建系统消息失败：未找到请求者或接受者。RequesterId: {RequesterId}, AddresseeId: {AddresseeId}",
                notification.RequesterId, notification.AddresseeId);
            return;
        }

        // 创建双方的系统消息
        try
        {
            // 为请求者创建系统消息
            var systemMessageToRequester = Message.CreateSystemMessage(
                addressee.Id, // 系统消息上下文中显示为来自另一方
                requester.Id,
                $"您已与 {addressee.Username} 成为好友。"
            );
            await _messageRepository.AddAsync(systemMessageToRequester, cancellationToken);
            
            // 为接受者创建系统消息
            var systemMessageToAddressee = Message.CreateSystemMessage(
                requester.Id, // 系统消息上下文中显示为来自另一方
                addressee.Id,
                $"您已与 {requester.Username} 成为好友。"
            );
            await _messageRepository.AddAsync(systemMessageToAddressee, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("已为新建立的好友关系创建系统消息。FriendshipId: {FriendshipId}", notification.FriendshipId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "为好友请求接受事件创建系统消息时出错。FriendshipId: {FriendshipId}", notification.FriendshipId);
        }
    }
}