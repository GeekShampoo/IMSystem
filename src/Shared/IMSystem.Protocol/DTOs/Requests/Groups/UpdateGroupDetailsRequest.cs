using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.Groups;

/// <summary>
/// 更新群组详细信息的请求DTO。
/// </summary>
public class UpdateGroupDetailsRequest
{
    /// <summary>
    /// 新的群组名称。如果为null，则不更新。
    /// </summary>
    [StringLength(100, MinimumLength = 3, ErrorMessage = "群组名称长度必须在 {2} 到 {1} 个字符之间。")]
    public string? Name { get; set; }

    /// <summary>
    /// 新的群组描述。如果为null，则不更新。
    /// </summary>
    [StringLength(500, ErrorMessage = "群组描述不能超过 {1} 个字符。")]
    public string? Description { get; set; }

    /// <summary>
    /// 新的群组头像URL。如果为null，则不更新。
    /// </summary>
    [Url(ErrorMessage = "无效的头像URL格式。")]
    public string? AvatarUrl { get; set; }
}