using System;
using IMSystem.Protocol.Enums; // For ApiErrorCode
 
namespace IMSystem.Server.Domain.Exceptions
{
    /// <summary>
    /// 表示在领域逻辑执行期间发生的错误的基类。
    /// 领域异常通常表示业务规则的违反或领域内不一致的状态。
    /// </summary>
    public class DomainException : Exception
    {
        /// <summary>
        /// 获取此领域异常关联的 API 错误码。
        /// 默认为 <see cref="ApiErrorCode.DomainRuleViolated"/>。
        /// 子类可以重写此属性以提供更具体的错误码。
        /// </summary>
        public virtual ApiErrorCode ErrorCode { get; } = ApiErrorCode.DomainRuleViolated; // 默认错误码更改为 DomainRuleViolated

        /// <summary>
        /// 初始化 <see cref="DomainException"/> 类的新实例。
        /// </summary>
        public DomainException()
        {
        }
 
        /// <summary>
        /// 使用指定的错误消息初始化 <see cref="DomainException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        public DomainException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// 使用指定的错误消息和 API 错误码初始化 <see cref="DomainException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        /// <param name="errorCode">与此异常关联的 API 错误码。</param>
        protected DomainException(string message, ApiErrorCode errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
 
        /// <summary>
        /// 使用指定的错误消息和对导致此异常的内部异常的引用来初始化 <see cref="DomainException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则为 null。</param>
        public DomainException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// 使用指定的错误消息、API 错误码和对导致此异常的内部异常的引用来初始化 <see cref="DomainException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        /// <param name="errorCode">与此异常关联的 API 错误码。</param>
        /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则为 null。</param>
        protected DomainException(string message, ApiErrorCode errorCode, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}