namespace IMSystem.Protocol.Enums
{
    /// <summary>
    /// Defines the type of a message in the protocol.
    /// </summary>
    public enum ProtocolMessageType
    {
        /// <summary>
        /// A plain text message.
        /// </summary>
        Text = 1,

        /// <summary>
        /// An image message.
        /// </summary>
        Image = 2,

        /// <summary>
        /// A file message.
        /// </summary>
        File = 3,

        /// <summary>
        /// An audio message.
        /// </summary>
        Audio = 4,

        /// <summary>
        /// A video message.
        /// </summary>
        Video = 5,

        /// <summary>
        /// A system notification message (e.g., user joined group).
        /// </summary>
        System = 100,

        /// <summary>
        /// A message indicating a call (audio/video).
        /// </summary>
        Call = 101,

        /// <summary>
        /// A recalled message placeholder.
        /// </summary>
        Recalled = 102
    }
}