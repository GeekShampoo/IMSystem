using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using IMSystem.Protocol.Common;
using IMSystem.Server.Core.Interfaces.Persistence;
using IMSystem.Server.Domain.Entities;
using IMSystem.Server.Domain.Events.Messages;
using IMSystem.Server.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using IMSystem.Server.Core.Settings;
using Microsoft.Extensions.Options;

namespace IMSystem.Server.Core.Features.Messages.Commands
{
    /// <summary>
    /// Handles the <see cref="EditMessageCommand"/>.
    /// </summary>
    public class EditMessageCommandHandler : IRequestHandler<EditMessageCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<EditMessageCommandHandler> _logger;
        private readonly MessageSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditMessageCommandHandler"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work.</param>
        /// <param name="logger">The logger.</param>
        public EditMessageCommandHandler(IUnitOfWork unitOfWork, ILogger<EditMessageCommandHandler> logger, IOptions<MessageSettings> options)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Handles the command to edit a message.
        /// </summary>
        /// <param name="request">The command request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A result indicating success or failure.</returns>
        public async Task<Result> Handle(EditMessageCommand request, CancellationToken cancellationToken)
        {
            var message = await _unitOfWork.MessageRepository.GetByIdAsync(request.MessageId);

            if (message == null)
            {
                _logger.LogWarning("Message with ID {MessageId} not found for editing.", request.MessageId);
                return Result.Failure(new Error("Message.NotFound", "Message not found."));
            }

            // UserId is already a Guid, no need for Guid.TryParse
            var userIdGuid = request.UserId;

            // Authorization: Check if the user is the sender and if the message is within the editable time window
            if (!message.SenderId.HasValue || message.SenderId.Value != userIdGuid)
            {
                _logger.LogWarning("User {UserId} attempted to edit message {MessageId} not sent by them (SenderId: {SenderId}).", request.UserId, request.MessageId, message.SenderId);
                return Result.Failure(new Error("Message.Forbidden", "You are not authorized to edit this message."));
            }

            if (message.SentAt.AddMinutes(_settings.EditTimeWindowMinutes) < DateTimeOffset.UtcNow)
            {
                _logger.LogInformation("User {UserId} attempted to edit message {MessageId} outside the allowed time window.", request.UserId, request.MessageId);
                return Result.Failure(new Error("Message.EditTimeExpired", $"Messages can only be edited within {_settings.EditTimeWindowMinutes} minutes of sending."));
            }

            // Update message content using the entity's method
            message.UpdateContentAndType(request.NewContent, message.Type, userIdGuid);
            // LastModifiedAt is handled by UpdateContentAndType
            // Potentially add a flag like IsEdited = true if needed for UI

            _unitOfWork.MessageRepository.Update(message);

            // Add domain event for message edited
            message.AddDomainEvent(new MessageEditedEvent(message));

            try
            {
                await _unitOfWork.CompleteAsync(cancellationToken);
                _logger.LogInformation("Message {MessageId} edited successfully by user {UserId}.", request.MessageId, request.UserId);
                return Result.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while editing message {MessageId}.", request.MessageId);
                // Consider more specific error handling or re-throwing if appropriate
                return Result.Failure(new Error("Message.EditFailed", "An unexpected error occurred while editing the message."));
            }
        }
    }
}