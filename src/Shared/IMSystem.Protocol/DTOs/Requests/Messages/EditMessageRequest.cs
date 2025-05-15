using System;

namespace IMSystem.Protocol.DTOs.Requests.Messages
{
    /// <summary>
    /// Represents a request to edit a message.
    /// </summary>
    public class EditMessageRequest
    {
        /// <summary>
        /// Gets or sets the ID of the message to be edited.
        /// </summary>
        public Guid MessageId { get; set; }

        /// <summary>
        /// Gets or sets the new content of the message.
        /// </summary>
        public string Content { get; set; }
    }
}