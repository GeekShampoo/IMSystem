using IMSystem.Protocol.Common;
using MediatR;
using System;

namespace IMSystem.Server.Core.Features.Groups.Commands;

/// <summary>
/// 创建新群组的命令。
/// </summary>
public record CreateGroupCommand(
    Guid CreatorUserId,
    string Name,
    string? Description,
    string? AvatarUrl) : IRequest<Result<Guid>>; // 返回新群组的ID