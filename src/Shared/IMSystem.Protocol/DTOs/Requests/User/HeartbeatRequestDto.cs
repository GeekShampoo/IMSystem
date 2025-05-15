using System;

namespace IMSystem.Protocol.DTOs.Requests.User
{
    /// <summary>
    /// 用户心跳请求 DTO
    /// 简化版本，不包含冗余参数
    /// </summary>
    public class HeartbeatRequestDto
    {
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}