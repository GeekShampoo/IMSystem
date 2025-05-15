using System;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using IMSystem.Protocol.Common;
using IMSystem.Protocol.Enums;

namespace IMSystem.Server.Web.Common
{
    /// <summary>
    /// API 错误响应工厂类，用于创建标准化的错误响应
    /// </summary>
    public static class ApiErrorFactory
    {
        /// <summary>
        /// 根据错误码创建标准的 API 错误响应
        /// </summary>
        /// <param name="errorCode">API 错误码</param>
        /// <param name="detail">错误详情（可选）</param>
        /// <param name="traceId">跟踪 ID（可选）</param>
        /// <param name="instance">错误发生的请求路径（可选）</param>
        /// <returns>标准化的 API 错误响应</returns>
        public static ApiErrorResponse Create(ApiErrorCode errorCode, string detail = null, string traceId = null, string instance = null, string message = null)
        {
            // 获取错误码对应的 HTTP 状态码
            var statusCode = GetStatusCodeForErrorCode(errorCode);

            // 获取错误码的描述信息作为错误标题
            var title = GetEnumDescription(errorCode);

            var response = new ApiErrorResponse(statusCode, title)
            {
                ErrorCode = ((int)errorCode).ToString(),
                Detail = detail,
                TraceId = traceId,
                Instance = instance,
                Type = $"https://imsystem.error-types/error-{(int)errorCode}"
            };

            return response;
        }

        /// <summary>
        /// 获取枚举值的描述信息
        /// </summary>
        private static string GetEnumDescription(Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            if (field == null) return value.ToString();

            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
            return attribute == null ? value.ToString() : attribute.Description;
        }

        /// <summary>
        /// 根据错误码获取对应的 HTTP 状态码
        /// </summary>
        private static int GetStatusCodeForErrorCode(ApiErrorCode errorCode)
        {
            // 根据错误码范围映射到合适的 HTTP 状态码
            int code = (int)errorCode;

            return code switch
            {
                // 通用错误 (1000-1999)
                1000 => (int)HttpStatusCode.InternalServerError, // 未知错误
                1001 => (int)HttpStatusCode.InternalServerError, // 服务器内部错误
                1002 => (int)HttpStatusCode.BadRequest,         // 请求参数验证失败
                1003 => (int)HttpStatusCode.NotFound,           // 请求的资源不存在
                1004 => (int)HttpStatusCode.Forbidden,          // 操作被拒绝，权限不足
                1005 => (int)HttpStatusCode.Unauthorized,       // 认证失败
                1006 => (int)HttpStatusCode.Conflict,           // 并发修改冲突

                // 用户相关错误 (2000-2999)
                >= 2000 and < 3000 => (int)HttpStatusCode.BadRequest,

                // 好友相关错误 (3000-3999)
                >= 3000 and < 4000 => (int)HttpStatusCode.BadRequest,

                // 群组相关错误 (4000-4999)
                >= 4000 and < 5000 => (int)HttpStatusCode.BadRequest,

                // 消息相关错误 (5000-5999)
                >= 5000 and < 6000 => (int)HttpStatusCode.BadRequest,

                // 文件相关错误 (6000-6999)
                6000 => 413,                                    // 文件大小超出限制 (Payload Too Large - 413)
                6001 => (int)HttpStatusCode.UnsupportedMediaType, // 不支持的文件类型
                6004 => 507,                                    // 存储空间不足 (Insufficient Storage - 507)
                >= 6000 and < 7000 => (int)HttpStatusCode.BadRequest,

                // 信令相关错误 (7000-7999)
                >= 7000 and < 8000 => (int)HttpStatusCode.BadRequest,

                // 默认为 400 Bad Request
                _ => (int)HttpStatusCode.BadRequest
            };
        }
    }
}