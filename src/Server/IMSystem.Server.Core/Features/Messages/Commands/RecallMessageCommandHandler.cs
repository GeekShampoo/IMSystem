using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Enums; // Added for MessageType, MessageRecipientType
using IMSystem.Server.Domain.Events.Messages; // For MessageRecalledEvent
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using IMSystem.Server.Core.Settings;
using Microsoft.Extensions.Options;

namespace IMSystem.Server.Core.Features.Messages.Commands;

public class RecallMessageCommandHandler : IRequestHandler<RecallMessageCommand, Result>
{
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublisher _publisher;
    private readonly ILogger<RecallMessageCommandHandler> _logger;
    private readonly TimeSpan _recallTimeLimit;
    private readonly MessageSettings _settings;

    public RecallMessageCommandHandler(
        IMessageRepository messageRepository,
        IUnitOfWork unitOfWork,
        IPublisher publisher,
        ILogger<RecallMessageCommandHandler> logger,
        IOptions<MessageSettings> options)
    {
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
        _logger = logger;
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _recallTimeLimit = TimeSpan.FromMinutes(_settings.RecallTimeWindowMinutes);
    }

    public async Task<Result> Handle(RecallMessageCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User {ActorUserId} attempting to recall message {MessageId}", request.ActorUserId, request.MessageId);

        var message = await _messageRepository.GetByIdAsync(request.MessageId);

        if (message == null)
        {
            _logger.LogWarning("Message {MessageId} not found for recall attempt by user {ActorUserId}.", request.MessageId, request.ActorUserId);
            return Result.Failure("Message.NotFound", "Message not found.");
        }

        if (message.CreatedBy != request.ActorUserId)
        {
            _logger.LogWarning("User {ActorUserId} attempted to recall message {MessageId} not sent by them. Sender: {SenderId}",
                request.ActorUserId, request.MessageId, message.CreatedBy);
            return Result.Failure("Message.Recall.AccessDenied", "You can only recall messages you sent.");
        }

        if (message.IsRecalled)
        {
            _logger.LogInformation("Message {MessageId} was already recalled.", request.MessageId);
            return Result.Success(); // Already recalled, treat as success.
        }
        
        // Check if the message type is System. System messages typically cannot be recalled.
        if (message.Type == MessageType.System)
        {
            _logger.LogWarning("User {ActorUserId} attempted to recall a system message {MessageId}. Action denied.", request.ActorUserId, request.MessageId);
            return Result.Failure("Message.Recall.SystemMessage", "System messages cannot be recalled.");
        }

        if (DateTimeOffset.UtcNow > message.CreatedAt.Add(_recallTimeLimit))
        {
            _logger.LogWarning("Recall time limit exceeded for message {MessageId}. SentAt: {SentAt}, Limit: {LimitMinutes} mins",
                request.MessageId, message.CreatedAt, _recallTimeLimit.TotalMinutes);
            return Result.Failure("Message.Recall.TimeLimitExceeded", $"Message recall time limit of {_recallTimeLimit.TotalMinutes} minutes exceeded.");
        }

        try
        {
            // The Recall method in the entity now handles the logic and updates IsRecalled, RecalledAt, Type, LastModifiedAt, LastModifiedBy
            // It also should handle the Content update if desired (e.g., to "[Message Recalled]")
            // For now, the entity's Recall method just marks it and changes type.
            // Let's assume the entity's Recall method is: message.Recall(request.ActorUserId, _recallTimeLimit);
            // but it should not re-check the time limit, that's a handler concern.
            // The entity method should just perform the state change if allowed.

            // Simplified: The entity's Recall method should just do the state change.
            // The handler does the permission and time limit checks.
            
            // Re-fetch the message to ensure we have the latest state before updating,
            // though for this operation, if it passed prior checks, it should be fine.
            // The entity's Recall method should be:
            // public void RecallMessage(Guid actorId) { ... sets IsRecalled, RecalledAt, Type, LastModified... }
            // For now, we use the one I defined: message.Recall(actorId, timeLimit)
            // which re-checks time limit. This is okay but slightly redundant.

            if (!message.Recall(request.ActorUserId, _recallTimeLimit)) // This will re-check time limit, which is fine.
            {
                // This path might be hit if there's a race condition or if the entity's internal logic
                // for recall has further constraints not covered by the handler's initial checks.
                // Given the current entity logic, this would primarily be due to time limit again or not being sender.
                _logger.LogWarning("Message recall failed for message {MessageId} by user {ActorUserId} despite initial checks. This might indicate a race condition or an unexpected state.",
                    request.MessageId, request.ActorUserId);
                return Result.Failure("Message.Recall.Failed", "Failed to recall message. It might have been modified or the recall window just closed.");
            }
            
            // _messageRepository.Update(message); // EF Core tracks changes

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Message {MessageId} successfully recalled by user {ActorUserId}.", request.MessageId, request.ActorUserId);

            var recalledEvent = new MessageRecalledEvent(
                message.Id,
                message.CreatedBy!.Value, // SenderId
                message.RecipientId,
                message.RecipientType,
                request.ActorUserId,
                message.RecalledAt!.Value // RecalledAt is set by the Recall method
            );
            // 禁止直接 Publish，统一通过实体 AddDomainEvent 添加领域事件
            message.AddDomainEvent(recalledEvent);
            // await _publisher.Publish(recalledEvent, cancellationToken);
            _logger.LogInformation("Published MessageRecalledEvent for MessageId: {MessageId}", message.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalling message {MessageId} by user {ActorUserId}.", request.MessageId, request.ActorUserId);
            return Result.Failure("Message.Recall.UnexpectedError", "An error occurred while recalling the message.");
        }
    }
}