using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.Files;

/// <summary>
/// Request DTO for initiating a file upload and obtaining a pre-signed URL.
/// </summary>
public class RequestFileUploadRequest
{
    /// <summary>
    /// The original name of the file to be uploaded.
    /// Example: "mydocument.pdf"
    /// </summary>
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The MIME content type of the file.
    /// Example: "application/pdf", "image/jpeg"
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 3)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// The size of the file in bytes.
    /// Used for validation and potentially for pre-signed URL generation constraints.
    /// </summary>
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "File size must be greater than 0.")] // Max size can be configured elsewhere
    public long FileSize { get; set; }
}