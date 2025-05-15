using System;
using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.FriendGroups;

/// <summary>
/// 将好友添加到指定分组的请求数据传输对象。
/// </summary>
public class AddFriendToGroupRequest
{
    /// <summary>
    /// 要添加到分组的好友关系ID (FriendshipId)。
    /// </summary>
    [Required(ErrorMessage = "好友关系ID不能为空。")]
    public Guid FriendshipId { get; set; }
}