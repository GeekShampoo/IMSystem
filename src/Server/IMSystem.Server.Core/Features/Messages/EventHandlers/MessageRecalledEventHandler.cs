using IMSystem.Protocol.DTOs.Notifications;
using IMSystem.Protocol.Enums;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Core.Interfaces.Services;
using IMSystem.Server.Domain.Events.Messages;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Features.Messages.EventHandlers;

public class MessageRecalledEventHandler : INotificationHandler<MessageRecalledEvent>
{
    private readonly ILogger<MessageRecalledEventHandler> _logger;
    private readonly IChatNotificationService _chatNotificationService;
    private readonly IGroupRepository _groupRepository; // To get group members if it's a group message

    public MessageRecalledEventHandler(
        ILogger<MessageRecalledEventHandler> logger,
        IChatNotificationService chatNotificationService,
        IGroupRepository groupRepository)
    {
        _logger = logger;
        _chatNotificationService = chatNotificationService;
        _groupRepository = groupRepository;
    }

    public async Task Handle(MessageRecalledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling MessageRecalledEvent for MessageId: {MessageId}. Sender: {SenderId}, Recipient: {RecipientId} ({RecipientType}), Actor: {ActorId}",
            notification.MessageId, notification.SenderId, notification.RecipientId, notification.RecipientType, notification.ActorId);

        // 使用规范化后的DTO
        var payload = new MessageRecalledNotificationDto
        {
            MessageId = notification.MessageId,
            SenderId = notification.SenderId,
            RecipientId = notification.RecipientId,
            RecipientType = (ProtocolMessageRecipientType)notification.RecipientType, // 将域模型枚举转换为协议层枚举
            ActorId = notification.ActorId, // 执行撤回操作的用户
            RecalledAt = notification.RecalledAt
        };

        string clientMethodName = "MessageRecalled"; 
        var userIdsToNotify = new List<string>();

        if (notification.RecipientType == IMSystem.Server.Domain.Enums.MessageRecipientType.User)
        {
            // Notify sender (actor) and recipient
            userIdsToNotify.Add(notification.SenderId.ToString());
            if (notification.SenderId != notification.RecipientId) // Avoid double-notifying if sender is also recipient (though unlikely for user messages)
            {
                userIdsToNotify.Add(notification.RecipientId.ToString());
            }
        }
        else if (notification.RecipientType == IMSystem.Server.Domain.Enums.MessageRecipientType.Group)
        {
            // Notify all members of the group
            var group = await _groupRepository.GetByIdWithMembersAsync(notification.RecipientId); // RecipientId is GroupId here
            if (group != null && group.Members != null && group.Members.Any())
            {
                userIdsToNotify.AddRange(group.Members.Select(m => m.UserId.ToString()));
            }
            else
            {
                _logger.LogWarning("Group {GroupId} not found or has no members to notify for message recall.", notification.RecipientId);
                return; // No one to notify
            }
        }

        if (!userIdsToNotify.Any())
        {
            _logger.LogInformation("No users to notify for MessageRecalledEvent for MessageId: {MessageId}", notification.MessageId);
            return;
        }
        
        // Remove duplicates just in case (e.g., sender is part of the group members list)
        userIdsToNotify = userIdsToNotify.Distinct().ToList();

        try
        {
            foreach (var userId in userIdsToNotify)
            {
                await _chatNotificationService.SendNotificationAsync(
                    userId,
                    clientMethodName,
                    payload,
                    cancellationToken);
            }
            
            _logger.LogInformation("Successfully sent MessageRecalled notification to {UserCount} users for MessageId: {MessageId}",
                userIdsToNotify.Count, notification.MessageId);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error sending MessageRecalled notification for MessageId: {MessageId}",
                notification.MessageId);
        }
    }
}