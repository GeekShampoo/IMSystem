using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.FriendGroups;

/// <summary>
/// 创建好友分组的请求数据传输对象。
/// </summary>
public class CreateFriendGroupRequest
{
    /// <summary>
    /// 好友分组的名称。
    /// </summary>
    [Required(ErrorMessage = "分组名称不能为空。")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "分组名称长度必须在 {2} 到 {1} 个字符之间。")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// 分组的排序序号 (可选，默认为0)。
    /// 序号越小，排序越靠前。
    /// </summary>
    public int Order { get; set; } = 0;
}