using MediatR;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Files; // For FileMetadataDto
using System;

namespace IMSystem.Server.Core.Features.Files.Queries;

/// <summary>
/// 根据ID获取文件元数据的查询。
/// </summary>
public class GetFileMetadataByIdQuery : IRequest<Result<FileMetadataDto>>
{
    /// <summary>
    /// 文件元数据的唯一标识符。
    /// </summary>
    public Guid FileMetadataId { get; }

    /// <summary>
    /// 发起查询请求的用户ID，用于权限检查。
    /// </summary>
    public Guid RequesterId { get; }

    public GetFileMetadataByIdQuery(Guid fileMetadataId, Guid requesterId)
    {
        FileMetadataId = fileMetadataId;
        RequesterId = requesterId;
    }
}