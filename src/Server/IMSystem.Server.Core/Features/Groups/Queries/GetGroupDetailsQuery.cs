using System;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Groups;
using MediatR;

namespace IMSystem.Server.Core.Features.Groups.Queries;

/// <summary>
/// 获取群组详细信息的查询。
/// </summary>
public record GetGroupDetailsQuery(
    Guid GroupId,
    Guid CurrentUserId,
    int PageNumber = 1,
    int PageSize = 20 // Default page size
) : IRequest<Result<GroupDto>>;