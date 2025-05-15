using System; // For Guid
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Added for validation attributes

namespace IMSystem.Protocol.DTOs.Requests.User
{
    public class BatchGetUsersRequest
    {
        // Changed to List<Guid> for type safety, assuming ExternalId is Guid.
        [Required(ErrorMessage = "用户外部ID列表不能为空。")]
        [MinLength(1, ErrorMessage = "至少需要一个用户外部ID。")]
        public List<Guid> UserExternalIds { get; set; } = new List<Guid>();
    }
}