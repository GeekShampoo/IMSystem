using System;

namespace IMSystem.Protocol.DTOs.Notifications
{
    /// <summary>
    /// Represents a notification for when a message has been edited.
    /// </summary>
    public class MessageEditedNotificationDto
    {
        /// <summary>
        /// Gets or sets the ID of the edited message.
        /// </summary>
        public Guid MessageId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the chat (e.g., user ID for private chat, group ID for group chat) where the message was edited.
        /// </summary>
        public string ChatId { get; set; } // Can be UserId for private chat or GroupId for group chat

        /// <summary>
        /// Gets or sets the new content of the message.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the message was edited.
        /// </summary>
        public DateTimeOffset EditedAt { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user who edited the message.
        /// </summary>
        public string EditedByUserId { get; set; }
    }
}