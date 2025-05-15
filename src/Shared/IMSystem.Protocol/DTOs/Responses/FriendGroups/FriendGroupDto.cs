using System;
using System.Collections.Generic; // For List
using IMSystem.Protocol.DTOs.Responses.Friends; // For FriendSummaryDto

namespace IMSystem.Protocol.DTOs.Responses.FriendGroups;

/// <summary>
/// 好友分组的数据传输对象。
/// </summary>
public class FriendGroupDto
{
    /// <summary>
    /// 分组的唯一标识符。
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// 分组所属用户的ID。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 分组名称。
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// 分组的排序序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 分组创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 指示此分组是否为用户的默认分组。
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 该分组下的好友列表。
    /// </summary>
    public List<FriendSummaryDto> Friends { get; set; } = new List<FriendSummaryDto>();
}