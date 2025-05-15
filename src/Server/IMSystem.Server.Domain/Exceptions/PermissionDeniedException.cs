using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Server.Domain.Exceptions
{
    /// <summary>
    /// 表示因权限不足而拒绝操作的异常。
    /// </summary>
    public class PermissionDeniedException : DomainException
    {
        /// <summary>
        /// 获取此异常关联的 API 错误码。
        /// </summary>
        public override ApiErrorCode ErrorCode { get; } = ApiErrorCode.AccessDenied;

        /// <summary>
        /// 初始化 <see cref="PermissionDeniedException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        public PermissionDeniedException(string message) : base(message)
        {
        }

        /// <summary>
        /// 使用指定的错误消息和对导致此异常的内部异常的引用来初始化 <see cref="PermissionDeniedException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则为 null。</param>
        public PermissionDeniedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}