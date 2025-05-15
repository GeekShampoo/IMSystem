using IMSystem.Server.Domain.Entities; // For FileMetadata
using IMSystem.Protocol.Common; // For Result
using System;
using System.IO;
using System.Threading; // Added for CancellationToken
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Services
{
    /// <summary>
    /// 定义文件存储服务的接口，用于处理文件上传、下载和删除等操作。
    /// </summary>
    public interface IFileStorageService
    {
        /// <summary>
        /// 获取文件存储提供程序的名称 (例如 "Local", "AzureBlob", "S3")。
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// 异步生成用于客户端上传文件的预签名URL。
        /// </summary>
        /// <param name="storedFileName">文件在存储系统中的唯一名称或键。</param>
        /// <param name="contentType">文件的MIME类型。</param>
        /// <param name="fileSize">文件大小（以字节为单位）。可选，但某些提供商可能需要或用于限制。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表示异步操作的任务。任务结果包含预签名URL和HTTP方法。</returns>
        Task<PreSignedUrlResult?> GetPresignedUploadUrlAsync(string storedFileName, string contentType, long? fileSize = null, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步确认文件上传完成。
        /// </summary>
        /// <param name="fileMetadataId">要确认上传的文件元数据的ID。</param>
        /// <param name="storedFileName">文件在存储系统中的实际文件名或键。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表示异步操作的任务。</returns>
        Task ConfirmUploadAsync(Guid fileMetadataId, string storedFileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步生成用于客户端下载文件的预签名URL。
        /// </summary>
        /// <param name="fileMetadataId">要生成下载URL的文件元数据的ID (用于记录或辅助验证)。</param>
        /// <param name="storedFileName">文件在存储系统中的唯一名称或键。</param>
        /// <param name="downloaderUserId">请求下载文件的用户的ID (用于权限检查和令牌生成)。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表示异步操作的任务。任务结果包含预签名下载URL；如果无法生成，则返回 null。</returns>
        Task<string?> GeneratePreSignedDownloadUrlAsync(Guid fileMetadataId, string storedFileName, Guid downloaderUserId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步直接上传文件（如果采用这种方式而不是预签名URL）。
        /// </summary>
        /// <param name="fileStream">包含文件数据的流。</param>
        /// <param name="fileName">原始文件名。</param>
        /// <param name="contentType">文件的MIME类型。</param>
        /// <param name="uploaderId">上传文件的用户的ID。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表示异步操作的任务。任务结果包含新创建的 <see cref="FileMetadata"/> 实体。</returns>
        Task<FileMetadata> UploadFileAsync(Stream fileStream, string fileName, string contentType, Guid uploaderId, CancellationToken cancellationToken = default);


        /// <summary>
        /// 异步删除物理文件。
        /// 此方法仅负责删除存储系统中的物理文件，不处理元数据或权限。
        /// </summary>
        /// <param name="storedFileName">文件在存储系统中的唯一名称或键。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>表示异步操作的任务。如果文件不存在或删除失败，则可能抛出异常。</returns>
        Task DeletePhysicalFileAsync(string storedFileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a publicly accessible URL for a stored file.
        /// This URL should be suitable for long-term access (e.g., for an avatar).
        /// </summary>
        /// <param name="storedFileName">The unique name or key of the file in the storage system.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the public URL, or null if a public URL cannot be generated.</returns>
        Task<string?> GetPublicUrlAsync(string storedFileName, System.Threading.CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles a file upload performed via a token, including token validation and file saving.
        /// </summary>
        /// <param name="targetFile">The unique name or key of the file in the storage system (from token payload).</param>
        /// <param name="contentType">The MIME type of the file (from token payload).</param>
        /// <param name="token">The validation token from the request.</param>
        /// <param name="expiresQueryParam">The expiration timestamp string from the request query.</param>
        /// <param name="requestBody">The stream containing the file data from the HTTP request body.</param>
        /// <param name="fileSize">Optional file size from the request.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A Result indicating success or failure.</returns>
        Task<Result> SaveUploadedFileViaTokenAsync(
            string targetFile,
            string contentType,
            string token,
            string expiresQueryParam,
            Stream requestBody,
            long? fileSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles a file download request via a token, including token validation and providing file stream.
        /// </summary>
        /// <param name="targetFile">The unique name or key of the file in the storage system (from token payload).</param>
        /// <param name="userId">The ID of the user requesting the download (from token payload).</param>
        /// <param name="token">The validation token from the request.</param>
        /// <param name="expiresQueryParam">The expiration timestamp string from the request query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A Result containing FileDownloadInfo if successful, or a failure result.</returns>
        Task<Result<FileDownloadInfo>> GetFileStreamViaTokenAsync(
            string targetFile,
            Guid userId,
            string token,
            string expiresQueryParam,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a file stream for download based on its stored information.
        /// This method assumes that necessary permissions have already been checked.
        /// </summary>
        /// <param name="storedFileName">The unique name or key of the file in the storage system.</param>
        /// <param name="originalFileName">The original name of the file, to be used for the download.</param>
        /// <param name="contentType">The MIME type of the file.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A Result containing FileDownloadInfo if successful, or a failure result.</returns>
        Task<Result<FileDownloadInfo>> GetFileStreamAsync(
            string storedFileName,
            string originalFileName,
            string contentType,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// 封装生成预签名URL操作的结果。
    /// </summary>
    public class PreSignedUrlResult
    {
        /// <summary>
        /// 获取或设置用于上传/下载文件的预签名URL。
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// 获取或设置客户端应与预签名URL一起使用的HTTP方法 (例如 "PUT", "GET")。
        /// </summary>
        public string HttpMethod { get; set; } = string.Empty;

        // 移除了 FileMetadataId 和 StoredFileNameSuggestion，因为它们在调用时已处理或生成。
    }

    /// <summary>
    /// Contains information needed to stream a file for download.
    /// </summary>
    public class FileDownloadInfo
    {
        /// <summary>
        /// Gets or sets the stream containing the file content.
        /// </summary>
        public Stream ContentStream { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of the file.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the original or suggested download file name.
        /// </summary>
        public string OriginalFileName { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public FileDownloadInfo() { } // Parameterless constructor for model binding or deserialization if needed
#pragma warning restore CS8618

        public FileDownloadInfo(Stream contentStream, string contentType, string originalFileName)
        {
            ContentStream = contentStream;
            ContentType = contentType;
            OriginalFileName = originalFileName;
        }
    }
}