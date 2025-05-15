using IMSystem.Protocol.Common;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.Messages.Commands;

/// <summary>
/// Command to recall a previously sent message.
/// </summary>
public class RecallMessageCommand : IRequest<Result>
{
    /// <summary>
    /// The ID of the message to be recalled.
    /// </summary>
    public Guid MessageId { get; }

    /// <summary>
    /// The ID of the user attempting to recall the message (must be the original sender).
    /// </summary>
    public Guid ActorUserId { get; }

    public RecallMessageCommand(Guid messageId, Guid actorUserId)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("Message ID cannot be empty.", nameof(messageId));
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("Actor user ID cannot be empty.", nameof(actorUserId));

        MessageId = messageId;
        ActorUserId = actorUserId;
    }
}