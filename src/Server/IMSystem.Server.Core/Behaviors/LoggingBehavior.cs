// filepath: f:\IMSystem\src\Server\IMSystem.Server.Core\Behaviors\LoggingBehavior.cs
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace IMSystem.Server.Core.Behaviors
{
    /// <summary>
    /// MediatR 管道行为，用于统一记录请求处理的日志。
    /// </summary>
    /// <typeparam name="TRequest">请求的类型。</typeparam>
    /// <typeparam name="TResponse">响应的类型。</typeparam>
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        /// <summary>
        /// 初始化 <see cref="LoggingBehavior{TRequest, TResponse}"/> 类的新实例。
        /// </summary>
        /// <param name="logger">用于记录日志的记录器。</param>
        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 处理指定的请求，并记录处理过程中的日志信息。
        /// </summary>
        /// <param name="request">要处理的请求。</param>
        /// <param name="next">表示管道中下一个处理程序的委托。</param>
        /// <param name="cancellationToken">用于取消异步操作的令牌。</param>
        /// <returns>表示异步操作的任务。任务结果包含来自下一个处理程序的响应。</returns>
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            
            // 由于请求可能包含敏感信息，这里只记录请求类型名称，不记录具体内容
            // 如果需要记录请求内容，应该实现适当的脱敏机制
            _logger.LogInformation("开始处理 {RequestName}", requestName);

            var stopwatch = Stopwatch.StartNew();
            TResponse response;
            
            try
            {
                response = await next();
                stopwatch.Stop();
                
                _logger.LogInformation("成功处理 {RequestName}，耗时 {ElapsedMilliseconds}ms", 
                    requestName, stopwatch.ElapsedMilliseconds);
                
                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(ex, "处理 {RequestName} 时发生错误，耗时 {ElapsedMilliseconds}ms", 
                    requestName, stopwatch.ElapsedMilliseconds);
                
                throw;
            }
        }
    }
}