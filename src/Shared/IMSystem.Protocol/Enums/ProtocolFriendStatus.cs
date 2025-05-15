namespace IMSystem.Protocol.Enums
{
    /// <summary>
    /// Defines the status of a friendship in the protocol.
    /// </summary>
    public enum ProtocolFriendStatus
    {
        /// <summary>
        /// Users are friends.
        /// </summary>
        Friends = 1,

        /// <summary>
        /// A friend request has been sent by the other user and is pending approval.
        /// </summary>
        PendingIncoming = 2,

        /// <summary>
        /// A friend request has been sent by the current user and is pending approval from the other user.
        /// </summary>
        PendingOutgoing = 3,

        /// <summary>
        /// The current user has blocked the other user.
        /// </summary>
        BlockedBySelf = 4,

        /// <summary>
        /// The current user has been blocked by the other user.
        /// </summary>
        BlockedByOther = 5,

        /// <summary>
        /// Not friends and no pending requests or blocks.
        /// </summary>
        None = 0
    }
}