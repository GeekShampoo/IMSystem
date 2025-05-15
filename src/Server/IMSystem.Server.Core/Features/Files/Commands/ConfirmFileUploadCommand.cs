using MediatR;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.DTOs.Responses.Files; // For FileMetadataDto (or a specific ConfirmFileUploadResponse)
using System;

namespace IMSystem.Server.Core.Features.Files.Commands;

/// <summary>
/// 确认文件上传已完成的命令。
/// </summary>
public class ConfirmFileUploadCommand : IRequest<Result<FileMetadataDto>> // 响应将是更新后的文件元数据
{
    /// <summary>
    /// 文件元数据的唯一标识符。
    /// </summary>
    public Guid FileMetadataId { get; }

    /// <summary>
    /// 执行确认操作的用户ID（通常是上传者）。
    /// </summary>
    public Guid ConfirmerId { get; }

    // 可选：客户端可以提供上传后的文件ETag或版本ID，用于服务端校验（如果存储服务支持）
    // public string? ETag { get; }

    /// <summary>
    /// Optional client-generated message ID, if this file upload is associated with a message being composed.
    /// </summary>
    public string? ClientMessageId { get; }

    public ConfirmFileUploadCommand(Guid fileMetadataId, Guid confirmerId, string? clientMessageId = null)
    {
        FileMetadataId = fileMetadataId;
        ConfirmerId = confirmerId;
        ClientMessageId = clientMessageId;
    }
}