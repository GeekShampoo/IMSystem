using MediatR;
using IMSystem.Protocol.Common; // For Result
using IMSystem.Protocol.DTOs.Responses.Files; // For RequestFileUploadResponse
using System;

namespace IMSystem.Server.Core.Features.Files.Commands;

/// <summary>
/// 请求文件上传并获取预签名URL的命令。
/// </summary>
public class RequestFileUploadCommand : IRequest<Result<RequestFileUploadResponse>>
{
    /// <summary>
    /// 文件的原始名称。
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// 文件的MIME内容类型。
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// 文件的大小（字节）。
    /// </summary>
    public long FileSize { get; }

    /// <summary>
    /// 发起上传请求的用户ID。
    /// </summary>
    public Guid RequesterId { get; }

    public RequestFileUploadCommand(string fileName, string contentType, long fileSize, Guid requesterId)
    {
        FileName = fileName;
        ContentType = contentType;
        FileSize = fileSize;
        RequesterId = requesterId;
    }
}