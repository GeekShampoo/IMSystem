using IMSystem.Server.Domain.Common;
using System;

namespace IMSystem.Server.Domain.Events.Files;

/// <summary>
/// Represents a domain event that is raised when a file upload has been confirmed
/// by the client and the server has updated the file's metadata accordingly.
/// </summary>
public class FileUploadConfirmedEvent : IMSystem.Server.Domain.Common.DomainEvent
{
    /// <summary>
    /// Gets the ID of the confirmed file metadata record.
    /// </summary>
    public Guid FileMetadataId { get; }

    /// <summary>
    /// Gets the original name of the file.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the name/key under which the file is stored.
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
    /// Gets the ID of the user who uploaded the file.
    /// </summary>
    public Guid UploaderId { get; }

    /// <summary>
    /// Gets the final access URL for the file, if available.
    /// </summary>
    public string? AccessUrl { get; }

    /// <summary>
    /// Gets the storage provider where the file is stored.
    /// </summary>
    public string StorageProvider { get; }

    /// <summary>
    /// Optional client-generated message ID, if this file upload is associated with a message being composed.
    /// </summary>
    public string? ClientMessageId { get; }

    public FileUploadConfirmedEvent(
        Guid fileMetadataId,
        string fileName,
        string storedFileName,
        string contentType,
        long fileSize,
        Guid uploaderId,
        string storageProvider,
        string? accessUrl,
        string? clientMessageId = null) // Added clientMessageId
    {
        FileMetadataId = fileMetadataId;
        FileName = fileName;
        StoredFileName = storedFileName;
        ContentType = contentType;
        FileSize = fileSize;
        UploaderId = uploaderId;
        StorageProvider = storageProvider;
        AccessUrl = accessUrl;
        ClientMessageId = clientMessageId; // Added clientMessageId
    }
}