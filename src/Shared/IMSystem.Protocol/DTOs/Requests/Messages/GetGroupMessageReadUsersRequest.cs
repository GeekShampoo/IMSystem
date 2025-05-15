using System;

namespace IMSystem.Protocol.DTOs.Requests.Messages
{
    /// <summary>
    /// Request to get the list of users who have read a specific group message.
    /// </summary>
    public class GetGroupMessageReadUsersRequest
    {
        /// <summary>
        /// The ID of the message.
        /// </summary>
        public Guid MessageId { get; set; }
    }
}