using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.User;

public class UpdateUserCustomStatusRequest
{
    /// <summary>
    /// The new custom status for the user.
    /// Max length 100 characters. Can be null or empty to clear the status.
    /// </summary>
    [MaxLength(100)]
    public string? CustomStatus { get; set; }
}