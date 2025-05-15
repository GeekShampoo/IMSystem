namespace IMSystem.Protocol.Enums
{
    /// <summary>
    /// Defines the role of a user within a group in the protocol.
    /// </summary>
    public enum ProtocolGroupUserRole
    {
        /// <summary>
        /// The owner of the group.
        /// </summary>
        Owner = 1,

        /// <summary>
        /// An administrator of the group.
        /// </summary>
        Admin = 2,

        /// <summary>
        /// A regular member of the group.
        /// </summary>
        Member = 3
    }
}