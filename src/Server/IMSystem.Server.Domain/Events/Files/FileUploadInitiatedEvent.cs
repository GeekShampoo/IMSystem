using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.Files;

/// <summary>
/// Represents a domain event that is raised when a file upload process is initiated,
/// typically after a pre-signed URL has been generated and an initial FileMetadata record created.
/// </summary>
public class FileUploadInitiatedEvent : IMSystem.Server.Domain.Common.DomainEvent // Explicitly specify the base class
{
    /// <summary>
    /// Gets the ID of the file metadata record.
    /// </summary>
    public Guid FileMetadataId { get; }

    /// <summary>
    /// Gets the original name of the file.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the name/key under which the file will be stored.
    /// </summary>
    public string StoredFileName { get; }

    /// <summary>
    /// Gets the content type of the file.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Gets the size of the file in bytes.
    /// </summary>
    public long FileSize { get; }

    /// <summary>
    /// Gets the ID of the user who initiated the upload.
    /// </summary>
    public Guid UploaderId { get; }

    /// <summary>
    /// Gets the pre-signed URL provided for uploading the file.
    /// This URL is typically short-lived and should not be stored long-term in the database
    /// but is useful for the event context if immediate post-processing or logging is needed.
    /// </summary>
    public string PreSignedUploadUrl { get; }


    public FileUploadInitiatedEvent(
        Guid fileMetadataId,
        string fileName,
        string storedFileName,
        string contentType,
        long fileSize,
        Guid uploaderId,
        string preSignedUploadUrl)
    {
        FileMetadataId = fileMetadataId;
        FileName = fileName;
        StoredFileName = storedFileName;
        ContentType = contentType;
        FileSize = fileSize;
        UploaderId = uploaderId;
        PreSignedUploadUrl = preSignedUploadUrl;
    }
}