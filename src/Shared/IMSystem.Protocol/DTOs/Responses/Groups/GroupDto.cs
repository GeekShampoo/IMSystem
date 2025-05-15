using System;
using System.Collections.Generic;
using IMSystem.Protocol.Common; // For PagedResult
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Responses.Groups;

/// <summary>
/// 群组信息的DTO。
/// </summary>
public class GroupDto
{
    /// <summary>
    /// 群组ID。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 群组名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 群组描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 群组头像URL。
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 群主的用户ID。
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// 群组创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 群组成员列表（可选，根据具体查询场景决定是否填充）。
    /// </summary>
    public PagedResult<GroupMemberDto>? Members { get; set; } // Changed to PagedResult

    /// <summary>
    /// 当前用户在群组中的角色（如果适用）。
    /// </summary>
    public ProtocolGroupUserRole? CurrentUserRole { get; set; }

    /// <summary>
    /// 获取或设置群组信息是否已与服务器同步（主要用于客户端本地存储）。
    /// </summary>
    public bool IsSynced { get; set; }
}