namespace IMSystem.Protocol.Common;

/// <summary>
/// Represents a standardized error.
/// </summary>
public record Error(string Code, string Message)
{
    /// <summary>
    /// Represents no error.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Implicit conversion from Error to string (returns Message).
    /// </summary>
    public static implicit operator string(Error error) => error.Message;
}