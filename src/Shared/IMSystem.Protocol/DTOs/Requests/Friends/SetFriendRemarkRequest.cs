using System;

namespace IMSystem.Protocol.DTOs.Requests.Friends
{
    /// <summary>
    /// Request to set a remark for a friend.
    /// </summary>
    public class SetFriendRemarkRequest
    {
        /// <summary>
        /// The user ID of the friend for whom the remark is being set.
        /// </summary>
        public Guid FriendUserId { get; set; }

        /// <summary>
        /// The remark to set for the friend. Can be null to clear the remark.
        /// </summary>
        public string? Remark { get; set; }
    }
}