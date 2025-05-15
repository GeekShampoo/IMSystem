using System;
using IMSystem.Server.Domain.Common;
using IMSystem.Server.Domain.Entities;

namespace IMSystem.Server.Domain.Events.Messages
{
    /// <summary>
    /// Represents an event that is raised when a message is edited.
    /// </summary>
    public class MessageEditedEvent : DomainEvent
    {
        /// <summary>
        /// Gets the edited message.
        /// </summary>
        public Message EditedMessage { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageEditedEvent"/> class.
        /// </summary>
        /// <param name="editedMessage">The message that was edited.</param>
        public MessageEditedEvent(Message editedMessage)
            : base(entityId: editedMessage.Id, triggeredBy: editedMessage.LastModifiedBy)
        {
            EditedMessage = editedMessage ?? throw new ArgumentNullException(nameof(editedMessage));
        }
    }
}