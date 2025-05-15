using System;

namespace IMSystem.Protocol.DTOs.Responses.Files;

/// <summary>
/// Response DTO after successfully initiating a file upload.
/// Contains the pre-signed URL for uploading and the file metadata ID.
/// </summary>
public class RequestFileUploadResponse
{
    /// <summary>
    /// The unique identifier for the created file metadata record.
    /// This ID should be used by the client to confirm the upload later.
    /// </summary>
    public Guid FileMetadataId { get; set; }

    /// <summary>
    /// The pre-signed URL that the client should use to upload the file directly
    /// to the storage provider (e.g., S3, Azure Blob Storage, or a local endpoint).
    /// </summary>
    public string PreSignedUrl { get; set; } = string.Empty;

    /// <summary>
    /// The HTTP method the client should use with the PreSignedUrl (e.g., "PUT", "POST").
    /// This is important as different storage providers might expect different methods.
    /// </summary>
    public string HttpMethod { get; set; } = "PUT"; // Default to PUT, common for S3/Azure

    // Optional: Any specific headers the client needs to include when making the upload request.
    // public Dictionary<string, string>? RequiredHeaders { get; set; }

    // Optional: Expiration time of the pre-signed URL (client might want to know this).
    // public DateTimeOffset? UrlExpiration { get; set; }
}