using System;
using System.Collections.Generic;
using IMSystem.Protocol.DTOs.Responses.User;

namespace IMSystem.Protocol.DTOs.Responses.Messages
{
    /// <summary>
    /// Response containing the list of users who have read a specific group message.
    /// </summary>
    public class GetGroupMessageReadUsersResponse
    {
        /// <summary>
        /// The ID of the message.
        /// </summary>
        public Guid MessageId { get; set; }

        /// <summary>
        /// The list of users who have read the message.
        /// </summary>
        public IEnumerable<UserSummaryDto> ReadUsers { get; set; }
    }
}