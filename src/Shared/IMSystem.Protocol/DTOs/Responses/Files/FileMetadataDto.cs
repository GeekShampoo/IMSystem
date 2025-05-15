using System;

namespace IMSystem.Protocol.DTOs.Responses.Files;

/// <summary>
/// 文件元数据的响应 DTO。
/// </summary>
public class FileMetadataDto
{
    /// <summary>
    /// 文件元数据的唯一标识符。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 文件的原始名称。
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件在存储系统中的名称或唯一键。
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件的MIME类型。
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 文件的大小，单位为字节。
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 上传此文件的用户ID。
    /// </summary>
    public Guid UploaderId { get; set; }

    /// <summary>
    /// 上传此文件的用户名 (可选, 方便客户端显示)。
    /// </summary>
    public string? UploaderUsername { get; set; }

    /// <summary>
    /// 文件上传的时间。
    /// </summary>
    public DateTimeOffset UploadedAt { get; set; }

    /// <summary>
    /// 文件存储提供程序的标识符。
    /// </summary>
    public string StorageProvider { get; set; } = string.Empty;

    /// <summary>
    /// 文件的可访问URL。
    /// </summary>
    public string? AccessUrl { get; set; }

    /// <summary>
    /// 指示文件上传是否已确认完成。
    /// </summary>
    public bool IsConfirmed { get; set; }

    /// <summary>
    /// 文件最后修改的时间。
    /// </summary>
    public DateTimeOffset? LastModifiedAt { get; set; }

    /// <summary>
    /// 获取或设置文件元数据是否已与服务器同步（主要用于客户端本地存储）。
    /// </summary>
    public bool IsSynced { get; set; }
}