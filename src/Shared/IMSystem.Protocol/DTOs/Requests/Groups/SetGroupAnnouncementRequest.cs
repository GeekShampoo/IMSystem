using System.ComponentModel.DataAnnotations;

namespace IMSystem.Protocol.DTOs.Requests.Groups;

/// <summary>
/// Request DTO for setting or updating a group's announcement.
/// </summary>
public class SetGroupAnnouncementRequest
{
    /// <summary>
    /// The new announcement text. 
    /// Send null or an empty string to clear the announcement.
    /// </summary>
    [StringLength(1000, ErrorMessage = "Announcement cannot exceed 1000 characters.")]
    public string? Announcement { get; set; }
}