using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IMSystem.Protocol.Common
{
    /// <summary>
    /// 统一的 API 错误响应模型，符合 RFC 7807 规范
    /// </summary>
    public class ApiErrorResponse
    {
        /// <summary>
        /// HTTP 状态码
        /// </summary>
        public int StatusCode { get; set; }
        
        /// <summary>
        /// 错误的简短描述
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 错误的详细描述
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Detail { get; set; }

        /// <summary>
        /// 错误发生的请求路径
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Instance { get; set; }

        /// <summary>
        /// 错误类型 URI
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Type { get; set; }

        /// <summary>
        /// 业务错误码
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ErrorCode { get; set; }

        /// <summary>
        /// 错误发生的时间
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// 跟踪标识符，用于关联日志
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TraceId { get; set; }

        /// <summary>
        /// 验证错误的详细信息
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IDictionary<string, string[]>? Errors { get; set; }

        public ApiErrorResponse(int statusCode, string title)
        {
            StatusCode = statusCode;
            Title = title;
            Timestamp = DateTimeOffset.UtcNow;
        }

        public ApiErrorResponse(int statusCode, string title, string? detail = null, string? errorCode = null)
            : this(statusCode, title)
        {
            Detail = detail;
            ErrorCode = errorCode;
        }
    }
}