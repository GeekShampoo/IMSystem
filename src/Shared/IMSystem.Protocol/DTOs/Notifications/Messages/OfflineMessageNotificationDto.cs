using System;
using System.Collections.Generic;
using IMSystem.Protocol.DTOs.Messages;

namespace IMSystem.Protocol.DTOs.Notifications
{
    /// <summary>
    /// 批量推送离线消息通知
    /// </summary>
    public class OfflineMessageNotificationDto
    {
        public Guid UserId { get; set; }
        public IEnumerable<MessageDto> Messages { get; set; }
        public DateTimeOffset PushTime { get; set; }
    }
}