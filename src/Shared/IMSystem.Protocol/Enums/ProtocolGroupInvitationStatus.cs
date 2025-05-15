namespace IMSystem.Protocol.Enums
{
    /// <summary>
    /// Defines the status of a group invitation in the protocol.
    /// </summary>
    public enum ProtocolGroupInvitationStatus
    {
        /// <summary>
        /// The invitation is pending a response.
        /// </summary>
        Pending = 1,

        /// <summary>
        /// The invitation has been accepted.
        /// </summary>
        Accepted = 2,

        /// <summary>
        /// The invitation has been rejected.
        /// </summary>
        Rejected = 3,

        /// <summary>
        /// The invitation has been cancelled by the inviter.
        /// </summary>
        Cancelled = 4,

        /// <summary>
        /// The invitation has expired.
        /// </summary>
        Expired = 5
    }
}