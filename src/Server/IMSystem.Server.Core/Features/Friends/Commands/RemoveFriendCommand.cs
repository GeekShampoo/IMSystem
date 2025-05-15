using MediatR;
using System;
using IMSystem.Protocol.Common;

namespace IMSystem.Server.Core.Features.Friends.Commands;

/// <summary>
/// 代表移除好友的命令。
/// </summary>
public record RemoveFriendCommand(Guid CurrentUserId, Guid FriendUserId) : IRequest<Result>;