// In: src/Shared/IMSystem.Protocol/DTOs/Notifications/Common/SignalRErrorDto.cs
namespace IMSystem.Protocol.DTOs.Notifications.Common
{
    public class SignalRErrorDto
    {
        public string Code { get; set; }
        public string Message { get; set; }

        // Parameterless constructor for deserialization, if needed
        public SignalRErrorDto()
        {
            Code = string.Empty;
            Message = string.Empty;
        }

        public SignalRErrorDto(string code, string message)
        {
            Code = code;
            Message = message;
        }
    }
}