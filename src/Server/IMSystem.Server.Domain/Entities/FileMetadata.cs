using IMSystem.Server.Domain.Common; // For AuditableEntity
using IMSystem.Server.Domain.Events; // For FileUploadConfirmedEvent
using IMSystem.Server.Domain.Exceptions; // For DomainException
using System;

namespace IMSystem.Server.Domain.Entities
{
    /// <summary>
    /// 代表已上传文件的元数据信息。
    /// </summary>
    public class FileMetadata : AuditableEntity // 继承自 AuditableEntity
    {
        private const int FileNameMaxLength = 255;
        private const int StoredFileNameMaxLength = 1024;
        private const int ContentTypeMaxLength = 100;
        private const int StorageProviderMaxLength = 50;

        /// <summary>
        /// 文件的原始名称。
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// 文件在存储系统中的名称或唯一键（例如，包含路径的完整文件名或对象存储的键）。
        /// </summary>
        public string StoredFileName { get; private set; }

        /// <summary>
        /// 文件的MIME类型 (例如 "image/jpeg", "application/pdf")。
        /// </summary>
        public string ContentType { get; private set; }

        /// <summary>
        /// 文件的大小，单位为字节。
        /// </summary>
        public long FileSize { get; private set; }

        // UploaderId 由 CreatedBy 表示
        // UploadedAt 由 CreatedAt 表示
        // Id, LastModifiedAt, LastModifiedBy 来自 AuditableEntity

        /// <summary>
        /// 导航属性，指向上传此文件的用户。
        /// CreatedBy 存储上传者ID。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public virtual User Uploader { get; private set; } // EF Core 会根据 CreatedBy (UploaderId) 关联
#pragma warning restore CS8618

        /// <summary>
        /// 文件存储提供程序的标识符 (例如 "AzureBlob", "S3", "LocalFileSystem")。
        /// </summary>
        public string StorageProvider { get; private set; }

        /// <summary>
        /// 文件的可访问URL（可选）。
        /// 这可能是文件的直接下载链接，或者是一个需要进一步处理（如生成预签名URL）的基础URL。
        /// </summary>
        public string? AccessUrl { get; private set; }

        /// <summary>
        /// 指示文件上传是否已由客户端或后端流程确认完成。
        /// 例如，在使用预签名URL上传时，客户端上传完成后需要调用API确认。
        /// </summary>
        public bool IsConfirmed { get; private set; }

        /// <summary>
        /// EF Core 使用的私有构造函数。
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private FileMetadata() : base() { } // 调用基类构造函数
#pragma warning restore CS8618

        /// <summary>
        /// 创建一个新的文件元数据实例。
        /// </summary>
        /// <param name="fileName">原始文件名。</param>
        /// <param name="storedFileName">存储系统中的文件名或键。</param>
        /// <param name="contentType">MIME类型。</param>
        /// <param name="fileSize">文件大小（字节）。</param>
        /// <param name="uploaderId">上传者用户ID。</param>
        /// <param name="storageProvider">存储提供程序标识符。</param>
        public FileMetadata(
            string fileName,
            string storedFileName,
            string contentType,
            long fileSize,
            Guid uploaderId,
            string storageProvider)
        {
            // Id 和 CreatedAt (UploadedAt) 由基类构造函数设置
            if (uploaderId == Guid.Empty)
                throw new ArgumentException("Uploader ID cannot be empty.", nameof(uploaderId));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty.", nameof(fileName));
            if (fileName.Length > FileNameMaxLength)
                throw new DomainException($"File name cannot exceed {FileNameMaxLength} characters.");

            if (string.IsNullOrWhiteSpace(storedFileName))
                throw new ArgumentException("Stored file name cannot be empty.", nameof(storedFileName));
            if (storedFileName.Length > StoredFileNameMaxLength)
                throw new DomainException($"Stored file name cannot exceed {StoredFileNameMaxLength} characters.");

            if (string.IsNullOrWhiteSpace(contentType))
                throw new ArgumentException("Content type cannot be empty.", nameof(contentType));
            if (contentType.Length > ContentTypeMaxLength)
                throw new DomainException($"Content type cannot exceed {ContentTypeMaxLength} characters.");

            if (fileSize < 0) // Allow 0 for empty files, though typically > 0
                throw new DomainException("File size cannot be negative.");

            if (string.IsNullOrWhiteSpace(storageProvider))
                throw new ArgumentException("Storage provider cannot be empty.", nameof(storageProvider));
            if (storageProvider.Length > StorageProviderMaxLength)
                throw new DomainException($"Storage provider name cannot exceed {StorageProviderMaxLength} characters.");

            CreatedBy = uploaderId; // 上传者即为创建者
            FileName = fileName;
            StoredFileName = storedFileName;
            ContentType = contentType;
            FileSize = fileSize;
            StorageProvider = storageProvider;
            IsConfirmed = false;       // 默认情况下，上传需要确认
            LastModifiedAt = CreatedAt; // 初始时 LastModifiedAt 等于 CreatedAt
            LastModifiedBy = uploaderId;

            // FileUploadedDomainEvent is typically raised after upload confirmation.
        }

        /// <summary>
        /// 确认文件上传已完成。
        /// </summary>
        /// <param name="confirmerId">执行确认操作的用户ID（通常是上传者或系统）。</param>
        /// <param name="accessUrl">文件的可访问URL（可选）。</param>
        /// <param name="clientMessageId">可选的客户端消息ID，用于关联。</param>
        public void ConfirmUpload(Guid confirmerId, string? accessUrl = null, string? clientMessageId = null)
        {
            if (!IsConfirmed)
            {
                IsConfirmed = true;
                AccessUrl = accessUrl ?? AccessUrl; // 如果提供了新的访问URL，则更新
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = confirmerId;

                // Explicitly define the event type to ensure no ambiguity
                var confirmedEvent = new IMSystem.Server.Domain.Events.Files.FileUploadConfirmedEvent(
                    fileMetadataId: this.Id,
                    fileName: this.FileName,
                    storedFileName: this.StoredFileName,
                    contentType: this.ContentType,
                    fileSize: this.FileSize,
                    uploaderId: this.CreatedBy ?? Guid.Empty, // CreatedBy is UploaderId, should not be null here
                    storageProvider: this.StorageProvider,
                    accessUrl: this.AccessUrl,
                    clientMessageId: clientMessageId // Pass clientMessageId to the event
                );
                AddDomainEvent(confirmedEvent);
            }
        }

        /// <summary>
        /// 更新文件的访问URL。
        /// </summary>
        /// <param name="accessUrl">新的访问URL。</param>
        /// <param name="modifierId">执行修改操作的用户ID。</param>
        public void UpdateAccessUrl(string? accessUrl, Guid modifierId) // accessUrl can be null to clear it
        {
            if (modifierId == Guid.Empty)
                throw new ArgumentException("Modifier ID cannot be empty.", nameof(modifierId));

            // Validate accessUrl if provided (e.g., not just whitespace, length, format)
            // Allowing null or empty string to clear the AccessUrl.
            if (accessUrl != null && string.IsNullOrWhiteSpace(accessUrl) && accessUrl.Length > 0) // If not null, but is whitespace (and not empty string)
            {
                 throw new DomainException("Access URL cannot be only whitespace if a value is provided.");
            }
            // Consider URL format validation if it's always expected to be a valid URL.
            // if (accessUrl != null && !string.IsNullOrEmpty(accessUrl) && !Uri.TryCreate(accessUrl, UriKind.Absolute, out _))
            // {
            //     throw new DomainException("Invalid Access URL format.");
            // }


            // TODO: 权限校验 (e.g., only uploader or admin can modify) should be handled in the Application Service layer.

            if (AccessUrl != accessUrl)
            {
                AccessUrl = accessUrl; // Allows setting to null or a new value
                LastModifiedAt = DateTimeOffset.UtcNow;
                LastModifiedBy = modifierId;
                // TODO: Consider a FileAccessUrlUpdatedDomainEvent
            }
        }
    }
}