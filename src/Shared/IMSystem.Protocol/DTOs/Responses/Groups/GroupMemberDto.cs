using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Responses.Groups;

/// <summary>
/// 群组成员信息的DTO。
/// </summary>
public class GroupMemberDto
{
    /// <summary>
    /// 用户ID。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 用户名。
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 用户昵称。
    /// </summary>
    public string? Nickname { get; set; }
    
    /// <summary>
    /// 用户头像URL。
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 在群组中的昵称。
    /// </summary>
    public string? NicknameInGroup { get; set; }

    /// <summary>
    /// 成员角色。
    /// </summary>
    public ProtocolGroupUserRole Role { get; set; }

    /// <summary>
    /// 加入时间。
    /// </summary>
    public DateTimeOffset JoinedAt { get; set; }

    /// <summary>
    /// 获取或设置群组成员信息是否已与服务器同步（主要用于客户端本地存储）。
    /// </summary>
    public bool IsSynced { get; set; }
}