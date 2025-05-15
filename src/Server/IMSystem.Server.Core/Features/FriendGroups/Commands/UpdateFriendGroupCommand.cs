using IMSystem.Protocol.Common; // For Result
using MediatR; // For IRequest and Unit
using System;

namespace IMSystem.Server.Core.Features.FriendGroups.Commands;

/// <summary>
/// 更新好友分组的命令。
/// </summary>
public class UpdateFriendGroupCommand : IRequest<Result> // 返回 Result 表示操作成功或失败
{
    /// <summary>
    /// 要更新的好友分组的ID。
    /// </summary>
    public Guid GroupId { get; }

    /// <summary>
    /// 执行此操作的用户ID (从认证上下文中获取)。
    /// </summary>
    public Guid CurrentUserId { get; }

    /// <summary>
    /// 新的分组名称 (如果为 null，则不更新名称)。
    /// </summary>
    public string? NewName { get; }

    /// <summary>
    /// 新的排序序号 (如果为 null，则不更新排序)。
    /// </summary>
    public int? NewOrder { get; }

    public UpdateFriendGroupCommand(Guid groupId, Guid currentUserId, string? newName, int? newOrder)
    {
        if (groupId == Guid.Empty)
            throw new ArgumentException("分组ID不能为空。", nameof(groupId));
        if (currentUserId == Guid.Empty)
            throw new ArgumentException("当前用户ID不能为空。", nameof(currentUserId));
        
        // 至少需要提供一个要更新的字段
        if (string.IsNullOrWhiteSpace(newName) && !newOrder.HasValue)
            throw new ArgumentException("至少需要提供新的分组名称或排序序号中的一个进行更新。", nameof(newName));

        if (newName != null && (newName.Length == 0 || newName.Length > 50)) // 与 DTO 验证一致
            throw new ArgumentOutOfRangeException(nameof(newName), "分组名称长度必须在1到50个字符之间。");
        
        if (newOrder.HasValue && newOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(newOrder), "排序序号必须大于或等于0。");

        GroupId = groupId;
        CurrentUserId = currentUserId;
        NewName = newName;
        NewOrder = newOrder;
    }
}