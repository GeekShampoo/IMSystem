using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Requests.User
{
    public class SearchUsersRequest
    {
        public string? Keyword { get; set; }
        public ProtocolGender? Gender { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}