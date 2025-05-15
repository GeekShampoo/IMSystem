using System;

namespace IMSystem.Protocol.DTOs.Notifications
{
    /// <summary>
    /// 密钥交换提议发送确认DTO
    /// </summary>
    public class KeyExchangeOfferSentConfirmationDto
    {
        /// <summary>
        /// 接收者用户ID
        /// </summary>
        public string RecipientUserId { get; set; } = string.Empty;
        
        /// <summary>
        /// 发送时间戳
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// 交换状态
        /// </summary>
        public string Status { get; set; } = "Sent";
    }
}