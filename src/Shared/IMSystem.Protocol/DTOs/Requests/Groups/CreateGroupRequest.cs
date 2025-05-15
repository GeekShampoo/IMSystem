using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.Groups;

/// <summary>
/// 创建新群组的请求DTO。
/// </summary>
public class CreateGroupRequest
{
    /// <summary>
    /// 群组名称。
    /// </summary>
    [Required(ErrorMessage = "群组名称不能为空。")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "群组名称长度必须在1到100个字符之间。")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 群组描述（可选）。
    /// </summary>
    [StringLength(500, ErrorMessage = "群组描述不能超过500个字符。")]
    public string? Description { get; set; }

    /// <summary>
    /// 群组头像 URL（可选）。
    /// </summary>
    [StringLength(2048, ErrorMessage = "头像URL过长。")]
    [Url(ErrorMessage = "无效的头像URL格式。")]
    public string? AvatarUrl { get; set; }

    // 可以在此添加初始成员列表，但为了简化初次实现，
    // 初始成员（特别是群主）将在后端自动处理。
    // public List<Guid>? InitialMemberUserIds { get; set; }
}