namespace IMSystem.Protocol.DTOs.Responses.Files;

/// <summary>
/// 确认文件上传完成后的响应 DTO。
/// </summary>
public class ConfirmFileUploadResponse
{
    /// <summary>
    /// 获取或设置已确认文件的元数据。
    /// </summary>
    public FileMetadataDto FileMetadata { get; set; }

    public ConfirmFileUploadResponse(FileMetadataDto fileMetadata)
    {
        FileMetadata = fileMetadata;
    }
}