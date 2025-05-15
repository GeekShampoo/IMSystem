using System.ComponentModel.DataAnnotations;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Requests.Messages
{
    public class MarkMessagesAsReadRequest
    {
        /// <summary>
        /// The ID of the chat partner (for user-to-user chat) or group (for group chat).
        /// </summary>
        [Required]
        public Guid ChatId { get; set; } // Could be UserId or GroupId depending on context

        /// <summary>
        /// Specifies if the ChatId refers to a "User" or "Group".
        /// </summary>
        [Required]
        public ProtocolChatType ChatType { get; set; }

        /// <summary>
        /// Optional. If provided, marks all messages up to and including this message ID as read.
        /// If not provided, and LastReadTimestamp is also not provided, it might imply marking all messages in the chat as read.
        /// </summary>
        public Guid? UpToMessageId { get; set; }

        /// <summary>
        /// Optional. If provided, marks all messages up to this timestamp as read.
        /// Useful if message IDs are not strictly sequential or if marking based on time is preferred.
        /// </summary>
        public DateTimeOffset? LastReadTimestamp { get; set; }
    }
}