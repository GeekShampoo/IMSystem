using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.FriendGroups;

/// <summary>
/// 更新好友分组的请求数据传输对象。
/// </summary>
public class UpdateFriendGroupRequest
{
    /// <summary>
    /// 新的好友分组名称。
    /// 如果不提供或为 null，则表示不修改名称。
    /// </summary>
    [StringLength(50, MinimumLength = 1, ErrorMessage = "分组名称长度必须在 {2} 到 {1} 个字符之间。")]
    public string? Name { get; set; }

    /// <summary>
    /// 新的分组排序序号。
    /// 如果不提供或为 null，则表示不修改排序序号。
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "排序序号必须大于或等于0。")]
    public int? Order { get; set; }
}