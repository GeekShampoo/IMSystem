using MediatR;
using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities; // For Message, MessageReadReceipt entities
using IMSystem.Server.Domain.Enums;   // For MessageRecipientType
using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Domain.Events; // For MessageReadEvent
using Microsoft.Extensions.Logging; // For ILogger
using System.Text.Json;
using IMSystem.Server.Domain.Events.Messages; // For JsonSerializer

namespace IMSystem.Server.Core.Features.Messages.Commands;

/// <summary>
/// 处理 MarkMessageAsReadCommand 的处理器。
/// </summary>
public class MarkMessageAsReadCommandHandler : IRequestHandler<MarkMessageAsReadCommand, Result>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IMessageReadReceiptRepository _receiptRepository;
    private readonly IOutboxRepository _outboxRepository; // 用于暂存领域事件
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MarkMessageAsReadCommandHandler> _logger;
    private readonly IGroupRepository _groupRepository; // 新增，用于群聊权限校验

    public MarkMessageAsReadCommandHandler(
        IMessageRepository messageRepository,
        IMessageReadReceiptRepository receiptRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        ILogger<MarkMessageAsReadCommandHandler> logger,
        IGroupRepository groupRepository) // 新增
    {
        _messageRepository = messageRepository ?? throw new ArgumentNullException(nameof(messageRepository));
        _receiptRepository = receiptRepository ?? throw new ArgumentNullException(nameof(receiptRepository));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository)); // 新增
    }

    public async Task<Result> Handle(MarkMessageAsReadCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "开始处理 MarkMessageAsReadCommand。ReaderUserId: {ReaderUserId}, ChatPartnerId: {ChatPartnerId}, GroupId: {GroupId}, UpToMessageId: {UpToMessageId}, LastReadTimestamp: {LastReadTimestamp}",
            request.ReaderUserId, request.ChatPartnerId, request.GroupId, request.UpToMessageId, request.LastReadTimestamp);

        List<Message> messagesToMarkAsRead = new List<Message>();
        bool isGroupChat = request.GroupId.HasValue;

        if (isGroupChat)
        {
            // 权限校验：用户是否为群组成员
            bool isMember = await _groupRepository.IsUserMemberOfGroupAsync(request.ReaderUserId, request.GroupId.Value);
            if (!isMember)
            {
                _logger.LogWarning("用户 {ReaderUserId} 不是群组 {GroupId} 的成员，无权标记消息已读。", request.ReaderUserId, request.GroupId.Value);
                return Result.Failure("Message.Read.AccessDenied", "无权操作此群组的消息。");
            }
            messagesToMarkAsRead = await _messageRepository.GetUnreadGroupMessagesUpToAsync(
                request.ReaderUserId,
                request.GroupId.Value,
                request.UpToMessageId,
                request.LastReadTimestamp,
                cancellationToken);
            _logger.LogInformation("获取到群聊 {GroupId} 中用户 {ReaderUserId} 的 {Count} 条未读消息进行处理。", request.GroupId.Value, request.ReaderUserId, messagesToMarkAsRead.Count);
        }
        else if (request.ChatPartnerId.HasValue)
        {
            messagesToMarkAsRead = await _messageRepository.GetUnreadUserMessagesUpToAsync(
               request.ReaderUserId,
               request.ChatPartnerId.Value,
               request.UpToMessageId,
               request.LastReadTimestamp,
               cancellationToken);
            _logger.LogInformation("获取到用户 {ReaderUserId} 与用户 {ChatPartnerId} 的 {Count} 条未读消息进行处理。", request.ReaderUserId, request.ChatPartnerId.Value, messagesToMarkAsRead.Count);
        }
        else
        {
            // 此情况已由命令构造函数中的验证阻止
            _logger.LogError("MarkMessageAsReadCommand 既没有 ChatPartnerId 也没有 GroupId。");
            return Result.Failure("Message.Read.InvalidParams", "无效的命令参数。");
        }

        if (!messagesToMarkAsRead.Any())
        {
            _logger.LogInformation("没有找到需要标记为已读的消息。ReaderUserId: {ReaderUserId}, ChatPartnerId: {ChatPartnerId}, GroupId: {GroupId}",
                request.ReaderUserId, request.ChatPartnerId, request.GroupId);
            return Result.Success(); // 没有消息需要标记，操作成功
        }

        var readAt = DateTimeOffset.UtcNow;
        var receiptsCreated = 0;
        var eventsToPublish = new List<OutboxMessage>();

        foreach (var message in messagesToMarkAsRead)
        {
            // 确保是接收者标记，而不是发送者自己标记自己的消息 (除非业务允许)
            // 对于单聊，消息的发送者是 message.CreatedBy，接收者是 message.RecipientId
            // 对于群聊，消息的发送者是 message.CreatedBy
            if (message.CreatedBy == request.ReaderUserId)
            {
                // _logger.LogInformation("用户 {ReaderUserId} 是消息 {MessageId} 的发送者，跳过标记已读。", request.ReaderUserId, message.Id);
                // continue; // 根据业务决定是否跳过
            }

            bool alreadyRead = await _receiptRepository.HasUserReadMessageAsync(message.Id, request.ReaderUserId, cancellationToken);
            if (alreadyRead)
            {
                _logger.LogInformation("消息 {MessageId} 已被用户 {ReaderUserId} 读取过，跳过。", message.Id, request.ReaderUserId);
                continue;
            }

            var receipt = MessageReadReceipt.Create(message.Id, request.ReaderUserId);
            await _receiptRepository.AddAsync(receipt, cancellationToken);
            receiptsCreated++;

            if (!message.CreatedBy.HasValue)
            {
                 _logger.LogWarning("消息 {MessageId} 的发送者ID (CreatedBy) 为空，无法创建 MessageReadEvent。", message.Id);
                 continue; // 跳过此消息的事件创建
            }

            var messageReadEvent = new MessageReadEvent(
                messageId: message.Id,
                readerUserId: request.ReaderUserId,
                readAt: readAt,
                senderUserId: message.CreatedBy.Value,
                recipientId: message.RecipientId, // 对于群聊，这是 GroupId
                recipientType: message.RecipientType // Changed from .ToString()
            );

            var eventType = messageReadEvent.GetType().FullName ?? messageReadEvent.GetType().Name;
            var eventPayload = JsonSerializer.Serialize(messageReadEvent, messageReadEvent.GetType());
            eventsToPublish.Add(new OutboxMessage(eventType, eventPayload, DateTime.UtcNow));
        }

        if (receiptsCreated > 0)
        {
            await _outboxRepository.AddRangeAsync(eventsToPublish); // 批量添加 OutboxMessage
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("成功为用户 {ReaderUserId} 标记了 {ReceiptsCreated} 条消息为已读。", request.ReaderUserId, receiptsCreated);
        }
        else
        {
            _logger.LogInformation("没有新的消息被标记为已读 (可能都已读或无符合条件的消息)。");
        }
        
        return Result.Success();
    }
}