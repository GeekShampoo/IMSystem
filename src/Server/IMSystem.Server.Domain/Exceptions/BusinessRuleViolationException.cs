using System;
using IMSystem.Protocol.Enums;

namespace IMSystem.Server.Domain.Exceptions
{
    /// <summary>
    /// 表示违反业务规则的异常。
    /// </summary>
    public class BusinessRuleViolationException : DomainException
    {
        /// <summary>
        /// 获取此异常关联的 API 错误码。
        /// </summary>
        public override ApiErrorCode ErrorCode { get; }

        /// <summary>
        /// 使用指定的错误消息和默认的业务规则冲突错误码初始化 <see cref="BusinessRuleViolationException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        public BusinessRuleViolationException(string message)
            : base(message, ApiErrorCode.BusinessRuleViolation) // 使用我们新添加的枚举成员
        {
            ErrorCode = ApiErrorCode.BusinessRuleViolation;
        }

        /// <summary>
        /// 使用指定的错误消息和特定的 API 错误码初始化 <see cref="BusinessRuleViolationException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        /// <param name="errorCode">与此业务规则冲突关联的特定 API 错误码。</param>
        public BusinessRuleViolationException(string message, ApiErrorCode errorCode)
            : base(message, errorCode)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// 使用指定的错误消息、默认的业务规则冲突错误码和对导致此异常的内部异常的引用来初始化 <see cref="BusinessRuleViolationException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则为 null。</param>
        public BusinessRuleViolationException(string message, Exception innerException)
            : base(message, ApiErrorCode.BusinessRuleViolation, innerException)
        {
            ErrorCode = ApiErrorCode.BusinessRuleViolation;
        }

        /// <summary>
        /// 使用指定的错误消息、特定的 API 错误码和对导致此异常的内部异常的引用来初始化 <see cref="BusinessRuleViolationException"/> 类的新实例。
        /// </summary>
        /// <param name="message">描述错误的消息。</param>
        /// <param name="errorCode">与此业务规则冲突关联的特定 API 错误码。</param>
        /// <param name="innerException">导致当前异常的异常；如果未指定内部异常，则为 null。</param>
        public BusinessRuleViolationException(string message, ApiErrorCode errorCode, Exception innerException)
            : base(message, errorCode, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}