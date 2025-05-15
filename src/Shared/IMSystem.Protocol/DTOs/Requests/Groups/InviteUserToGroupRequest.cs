using System;
using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.Groups;

/// <summary>
/// Request DTO for inviting a user to a group.
/// </summary>
public class InviteUserToGroupRequest
{
    /// <summary>
    /// The ID of the user to be invited.
    /// </summary>
    [Required(ErrorMessage = "被邀请用户ID不能为空。")]
    public Guid InvitedUserId { get; set; }

    /// <summary>
    /// Optional message to include with the invitation.
    /// </summary>
    [MaxLength(500, ErrorMessage = "邀请消息不能超过500个字符。")]
    public string? Message { get; set; }

    /// <summary>
    /// Optional duration in hours for which the invitation is valid.
    /// If null, the invitation might not expire or use a default system expiration.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "邀请有效小时数必须为正数。")]
    public int? ExpiresInHours { get; set; }
}