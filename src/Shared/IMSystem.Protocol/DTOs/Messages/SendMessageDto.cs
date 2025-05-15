using System;
using System.ComponentModel.DataAnnotations;
using IMSystem.Protocol.Enums;

namespace IMSystem.Protocol.DTOs.Messages
{
    /// <summary>
    /// 表示发送消息的请求数据传输对象 (通常由客户端发送给 Hub)。
    /// </summary>
    public class SendMessageDto
    {
        /// <summary>
        /// 获取或设置接收者的ID (可以是用户ID或群组ID)。
        /// </summary>
        [Required(ErrorMessage = "接收者ID不能为空。")]
        public Guid RecipientId { get; set; }

        /// <summary>
        /// 获取或设置接收者的类型 ("User" 或 "Group")。 建议使用枚举或常量字符串以提高类型安全。
        /// </summary>
        [Required(ErrorMessage = "接收者类型不能为空。")]
        public ProtocolMessageRecipientType RecipientType { get; set; }

        /// <summary>
        /// 获取或设置消息的内容。
        /// </summary>
        [Required(ErrorMessage = "消息内容不能为空。")]
        [StringLength(4000, ErrorMessage = "消息内容不能超过4000个字符。")]
        public string Content { get; set; }

        /// <summary>
        /// 获取或设置消息的类型 ("Text", "Image", "File" 等)。 建议使用枚举或常量字符串。
        /// </summary>
        [Required(ErrorMessage = "消息类型不能为空。")]
        public ProtocolMessageType MessageType { get; set; }

        /// <summary>
        /// 获取或设置客户端生成的消息ID（可选），用于去重或跟踪。
        /// </summary>
        public Guid? ClientMessageId { get; set; }

        /// <summary>
        /// 获取或设置回复的消息ID（可选）。
        /// </summary>
        public Guid? ReplyToMessageId { get; set; }
    }
}