namespace IMSystem.Protocol.Enums
{
    /// <summary>
    /// Defines the gender of a user in the protocol.
    /// </summary>
    public enum ProtocolGender
    {
        /// <summary>
        /// Gender is not specified or not set.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Male.
        /// </summary>
        Male = 1,

        /// <summary>
        /// Female.
        /// </summary>
        Female = 2,

        /// <summary>
        /// Other gender identity.
        /// </summary>
        Other = 3,

        /// <summary>
        /// User prefers not to say.
        /// </summary>
        PreferNotToSay = 4
    }
}