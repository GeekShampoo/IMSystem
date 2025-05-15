using MediatR;
using System;
using IMSystem.Protocol.Common; // Added for Result

namespace IMSystem.Server.Core.Features.Friends.Commands
{
    /// <summary>
    /// Command to set a remark for a friend.
    /// </summary>
    public class SetFriendRemarkCommand : IRequest<Result> // Changed from IRequest to IRequest<Result>
    {
        /// <summary>
        /// The ID of the current user who is setting the remark.
        /// </summary>
        public Guid CurrentUserId { get; set; }

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