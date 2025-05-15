using IMSystem.Protocol.Common; // For Result
using IMSystem.Protocol.DTOs.Responses.FriendGroups; // For FriendGroupDto as return type
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

/// <summary>
/// 创建好友分组的命令。
/// </summary>
public class CreateFriendGroupCommand : IRequest<Result<FriendGroupDto>> // 返回创建的 FriendGroupDto
{
    /// <summary>
    /// 创建分组的用户ID (从认证上下文中获取)。
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// 要创建的分组的名称。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 分组的排序序号 (可选)。
    /// </summary>
    public int Order { get; }

    public CreateFriendGroupCommand(Guid userId, string name, int order)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("用户ID不能为空。", nameof(userId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("分组名称不能为空。", nameof(name));
        if (name.Length > 50) // 与 DTO 验证一致
            throw new ArgumentOutOfRangeException(nameof(name), "分组名称长度不能超过50个字符。");
            
        UserId = userId;
        Name = name;
        Order = order;
    }
}