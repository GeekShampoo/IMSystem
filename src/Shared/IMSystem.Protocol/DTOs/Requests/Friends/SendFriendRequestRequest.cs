using System;
using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.Friends;

/// <summary>
/// 发送好友请求的数据传输对象。
/// </summary>
public class SendFriendRequestRequest
{
    /// <summary>
    /// 要发送好友请求的目标用户的ID。
    /// </summary>
    [Required(ErrorMessage = "目标用户ID不能为空。")]
    public Guid AddresseeId { get; set; }
    /// <summary>
    /// 请求者发送的备注/验证信息（可选）。
    /// </summary>
    [StringLength(200, ErrorMessage = "备注信息不能超过 {1} 个字符。")]
    public string? RequesterRemark { get; set; }
}