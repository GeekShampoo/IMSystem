using System;
using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.Groups;

/// <summary>
/// Request DTO for transferring group ownership.
/// </summary>
public class TransferGroupOwnershipRequest
{
    /// <summary>
    /// The ID of the user to whom ownership is being transferred.
    /// </summary>
    [Required]
    public Guid NewOwnerUserId { get; set; }
}