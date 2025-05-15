namespace IMSystem.Protocol.Enums
{
    /// <summary>
    /// Defines the recipient type for a message in the protocol.
    /// </summary>
    public enum ProtocolMessageRecipientType
    {
        /// <summary>
        /// The recipient is a single user.
        /// </summary>
        User = 1,

        /// <summary>
        /// The recipient is a group.
        /// </summary>
        Group = 2
    }
}