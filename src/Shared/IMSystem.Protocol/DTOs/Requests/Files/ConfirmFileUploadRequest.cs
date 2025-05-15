using System;
using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.Files;

/// <summary>
/// 客户端用于确认文件上传已完成的请求 DTO。
/// </summary>
public class ConfirmFileUploadRequest
{
    /// <summary>
    /// 获取或设置文件元数据的唯一标识符。
    /// 该 ID 是在请求上传URL时由服务端返回的。
    /// </summary>
    [Required]
    public Guid FileMetadataId { get; set; }
}