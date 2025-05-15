using System;
using MediatR;
using IMSystem.Protocol.Common;

namespace IMSystem.Server.Core.Features.Messages.Commands
{
    /// <summary>
    /// Represents a command to edit a message.
    /// </summary>
    public class EditMessageCommand : IRequest<Result>
    {
        /// <summary>
        /// Gets or sets the ID of the message to be edited.
        /// </summary>
        public Guid MessageId { get; set; }

        /// <summary>
        /// Gets or sets the new content of the message.
        /// </summary>
        public string NewContent { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user performing the edit.
        /// </summary>
        public Guid UserId { get; set; }
    }
}