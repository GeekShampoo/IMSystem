using System;
using System.ComponentModel.DataAnnotations;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Requests.User;

/// <summary>
/// Request DTO for updating a user's profile.
/// All fields are optional; only provided fields will be updated.
/// </summary>
public class UpdateUserProfileRequest
{
    [StringLength(100, ErrorMessage = "Nickname cannot exceed 100 characters.")]
    public string? Nickname { get; set; }

    [StringLength(2048, ErrorMessage = "Avatar URL cannot exceed 2048 characters.")]
    [Url(ErrorMessage = "Avatar URL must be a valid URL.")]
    public string? AvatarUrl { get; set; }

    [EnumDataType(typeof(ProtocolGender), ErrorMessage = "无效的性别值。")]
    public ProtocolGender? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(200, ErrorMessage = "Street cannot exceed 200 characters.")]
    public string? Street { get; set; }

    [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "State or Province cannot exceed 100 characters.")]
    public string? StateOrProvince { get; set; }

    [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters.")]
    public string? Country { get; set; }

    [StringLength(20, ErrorMessage = "Zip code cannot exceed 20 characters.")]
    public string? ZipCode { get; set; }

    [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
    public string? Bio { get; set; }
}