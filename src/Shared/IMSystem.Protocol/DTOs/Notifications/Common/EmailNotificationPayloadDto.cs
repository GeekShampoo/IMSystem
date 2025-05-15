using System;

namespace IMSystem.Protocol.DTOs.Notifications.Common
{
    /// <summary>
    /// 电子邮件通知负载DTO
    /// </summary>
    public class EmailNotificationPayloadDto
    {
        /// <summary>
        /// 接收者邮箱地址
        /// </summary>
        public string To { get; set; } = string.Empty;
        
        /// <summary>
        /// 邮件主题
        /// </summary>
        public string Subject { get; set; } = string.Empty;
        
        /// <summary>
        /// 邮件正文内容
        /// </summary>
        public string Body { get; set; } = string.Empty;
        
        /// <summary>
        /// 内容是否为HTML格式
        /// </summary>
        public bool IsHtml { get; set; } = true;
    }
}