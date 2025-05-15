using FluentValidation;
using IMSystem.Protocol.Common; // For Result<T>
using MediatR;
using Microsoft.Extensions.Logging;
using System.Linq; // For .First()
using System.Reflection; // For Type manipulation

namespace IMSystem.Server.Core.Behaviors
{
    /// <summary>
    /// MediatR 管道行为，用于运行 FluentValidation 验证器。
    /// </summary>
    /// <typeparam name="TRequest">请求的类型。</typeparam>
    /// <typeparam name="TResponse">响应的类型。</typeparam>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

        /// <summary>
        /// 初始化 <see cref="ValidationBehavior{TRequest, TResponse}"/> 类的新实例。
        /// </summary>
        /// <param name="validators">可用的请求验证器集合。</param>
        /// <param name="logger">用于记录验证信息的日志记录器。</param>
        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        {
            _validators = validators;
            _logger = logger;
        }

        /// <summary>
        /// 处理指定的请求，通过运行配置的验证器来验证请求。
        /// </summary>
        /// <param name="request">要处理的请求。</param>
        /// <param name="next">表示管道中下一个处理程序的委托。</param>
        /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
        /// <returns>表示异步操作的任务。任务结果包含来自下一个处理程序的响应，如果验证失败，则抛出异常。</returns>
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);

            _logger.LogInformation("----- 正在验证命令 {CommandType}", typeof(TRequest).Name);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Any())
            {
                _logger.LogWarning("Validation errors - {RequestType} - Request: {@Request} - Errors: {@ValidationErrors}", typeof(TRequest).Name, request, failures);

                // If TResponse is a Result<T>, return a Failure result.
                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var firstFailure = failures.First();
                    var errorCode = string.IsNullOrEmpty(firstFailure.ErrorCode) ? firstFailure.PropertyName : firstFailure.ErrorCode;
                    var errorMessage = firstFailure.ErrorMessage;
                    var error = new Error(errorCode, errorMessage);

                    // TResponse is Result<TActualValue>. We want to call Result.Failure<TActualValue>(error).
                    var actualResultType = typeof(TResponse).GetGenericArguments()[0]; // This is TActualValue

                    // Find the generic method definition Result.Failure<T>(Error error) on the non-generic Result class.
                    MethodInfo? genericFailureMethodDefinition = typeof(Result)
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(m =>
                            m.Name == "Failure" &&
                            m.IsGenericMethodDefinition &&
                            m.GetParameters().Length == 1 &&
                            m.GetParameters()[0].ParameterType == typeof(Error) &&
                            m.GetGenericArguments().Length == 1);

                    if (genericFailureMethodDefinition != null)
                    {
                        // Make the generic method concrete with actualResultType: Result.Failure<actualResultType>(Error error)
                        MethodInfo concreteFailureMethod = genericFailureMethodDefinition.MakeGenericMethod(actualResultType);
                        
                        // Invoke the static method: Result.Failure<actualResultType>(error)
                        object? failureResult = concreteFailureMethod.Invoke(null, new object[] { error });

                        if (failureResult is TResponse typedFailureResult)
                        {
                            return typedFailureResult;
                        }
                    }
                    else
                    {
                        // Log if the expected method isn't found, as this would be a setup issue.
                        _logger.LogError("Could not find the static generic method Result.Failure<T>(Error error). Falling back to exception.");
                    }
                }

                // If TResponse is not a Result<T> type, or if reflection failed, throw exception (original behavior for commands).
                throw new ValidationException(failures);
            }

            return await next();
        }
    }
}