using System;
using IMSystem.Protocol.Common;
using MediatR;

namespace IMSystem.Server.Core.Features.Groups.Commands;

/// <summary>
/// 更新群组详细信息的命令。
/// </summary>
public record UpdateGroupDetailsCommand(
    Guid GroupId,
    string? Name,
    string? Description,
    string? AvatarUrl,
    Guid UserId // 执行操作的用户ID，用于权限检查
) : IRequest<Result>;