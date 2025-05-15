using MediatR;
using IMSystem.Server.Domain.Events;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Protocol.DTOs.Notifications;
using IMSystem.Server.Domain.Enums;   // Added for MessageRecipientType
using IMSystem.Protocol.Enums;      // Added for ProtocolChatType
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using IMSystem.Server.Core.Interfaces.Persistence; // For IUserRepository (optional, if needed to get ReaderUsername)
using System;
using IMSystem.Server.Domain.Events.Messages;

namespace IMSystem.Server.Core.Features.Messages.Events;

/// <summary>
/// 处理 MessageReadEvent 的处理器。
/// </summary>
public class MessageReadEventHandler : INotificationHandler<MessageReadEvent>
{
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IUserRepository _userRepository; // 用于获取用户名等信息
    private readonly ILogger<MessageReadEventHandler> _logger;

    public MessageReadEventHandler(
        IChatNotificationService chatNotificationService,
        IUserRepository userRepository,
        ILogger<MessageReadEventHandler> logger)
    {
        _chatNotificationService = chatNotificationService ?? throw new ArgumentNullException(nameof(chatNotificationService));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Handle(MessageReadEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("处理 MessageReadEvent，消息ID: {MessageId}, 读取者ID: {ReaderUserId}",
            notification.MessageId, notification.ReaderUserId);

        string? readerUsername = null;
        try
        {
            var readerUser = await _userRepository.GetByIdAsync(notification.ReaderUserId);
            if (readerUser != null)
            {
                readerUsername = readerUser.Username;
            }
            else
            {
                _logger.LogWarning("未找到 MessageReadEvent 中的读取者用户信息，用户ID: {ReaderUserId}", notification.ReaderUserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 MessageReadEvent 获取读取者 {ReaderUserId} 用户名失败。", notification.ReaderUserId);
            // 即使获取用户名失败，也应该继续尝试发送通知，只是用户名可能为空
        }

        var notificationDto = new MessageReadNotificationDto
        {
            MessageId = notification.MessageId,
            ReaderUserId = notification.ReaderUserId,
            ReaderUsername = readerUsername, // 可能为 null
            ReadAt = notification.ReadAt,
            ConversationId = notification.RecipientType == MessageRecipientType.User ? notification.SenderUserId : notification.RecipientId, // 对于单聊，通知发送者，会话ID是发送者；对于群聊，会话ID是群ID
            ConversationType = notification.RecipientType == MessageRecipientType.User ? ProtocolChatType.Private : ProtocolChatType.Group
        };

        try
        {
            // 将已读通知发送给消息的发送者 (如果不是读取者自己)
            if (notification.SenderUserId != notification.ReaderUserId)
            {
                await _chatNotificationService.NotifyMessageReadAsync(notification.SenderUserId.ToString(), notificationDto, cancellationToken);
                _logger.LogInformation("已读通知已发送给消息发送者 {SenderUserId}，针对消息 {MessageId}", notification.SenderUserId, notification.MessageId);
            }

            // 如果是单聊，也需要通知读取者自己的其他客户端同步状态
            // 如果是群聊，理论上群内其他成员不需要知道“谁”读了，但发送者需要知道。
            // 如果业务需求是群内所有人都能看到谁读了，则需要推送给群组（但当前 DTO 和事件设计更偏向通知发送者）
            // 此处，我们通知读取者自己的其他客户端
            await _chatNotificationService.NotifyMessageReadAsync(notification.ReaderUserId.ToString(), notificationDto, cancellationToken);
            _logger.LogInformation("已读通知已发送给读取者 {ReaderUserId} (用于多端同步)，针对消息 {MessageId}", notification.ReaderUserId, notification.MessageId);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送消息已读通知失败，消息ID: {MessageId}, 读取者ID: {ReaderUserId}",
                notification.MessageId, notification.ReaderUserId);
            // 根据策略，这里可以考虑重试或记录到死信队列
        }
    }
}