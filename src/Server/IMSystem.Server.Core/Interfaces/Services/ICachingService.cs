using System;
using System.Threading; // Added for CancellationToken
using System.Threading.Tasks;

namespace IMSystem.Server.Core.Interfaces.Services
{
    /// <summary>
    /// 定义缓存服务的接口，用于抽象缓存操作。
    /// </summary>
    public interface ICachingService
    {
        /// <summary>
        /// 异步从缓存中获取指定键的项。
        /// </summary>
        /// <typeparam name="T">项的类型。</typeparam>
        /// <param name="key">缓存键，用于唯一标识缓存中的项。</param>
        /// <param name="refreshSlidingExpirationWith">如果提供此值且找到了键，则使用此 TimeSpan 刷新键的过期时间（模拟滑动过期）。</param>
        /// <param name="cancellationToken">用于观察取消请求的标记。</param>
        /// <returns>
        /// 表示异步操作的任务。任务结果是一个元组，包含一个布尔值，指示是否找到了键 (<c>Found</c>)，
        /// 以及缓存的项 (<c>Value</c>)；如果未找到，则 <c>Value</c> 为 null 或默认值。
        /// </returns>
        Task<(bool Found, T? Value)> GetAsync<T>(string key, TimeSpan? refreshSlidingExpirationWith = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步向缓存中设置具有指定键和值的项。
        /// </summary>
        /// <typeparam name="T">项的类型。</typeparam>
        /// <param name="key">缓存键，用于唯一标识缓存中的项。</param>
        /// <param name="value">要缓存的值。</param>
        /// <param name="absoluteExpirationRelativeToNow">
        /// 相对于现在的绝对过期时间。如果设置，则项将在指定时间后过期。
        /// 如果同时提供了 <paramref name="slidingExpiration"/>，则 <paramref name="slidingExpiration"/> 优先。
        /// </param>
        /// <param name="slidingExpiration">
        /// 滑动过期时间。如果提供，则项的初始过期时间将设置为此值。
        /// 要实现实际的滑动行为，调用者在通过 <see cref="GetAsync{T}"/> 获取此项时，
        /// 需要提供相同的 TimeSpan 值给 <c>refreshSlidingExpirationWith</c> 参数。
        /// 如果此参数有值，它将优先于 <paramref name="absoluteExpirationRelativeToNow"/>。
        /// </param>
        /// <param name="cancellationToken">用于观察取消请求的标记。</param>
        Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步从缓存中移除具有指定键的项。
        /// </summary>
        /// <param name="key">缓存键，用于唯一标识缓存中的项。</param>
        /// <param name="cancellationToken">用于观察取消请求的标记。</param>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步刷新缓存中具有指定键的项的过期时间（如果缓存提供程序支持）。
        /// 对于配置了滑动过期的项，这通常意味着将其过期时间延长。
        /// </summary>
        /// <param name="key">缓存键，用于唯一标识缓存中的项。</param>
        /// <param name="cancellationToken">用于观察取消请求的标记。</param>
        Task RefreshAsync(string key, CancellationToken cancellationToken = default); // Note: The utility of this method might diminish with the new GetAsync approach for sliding.

        /// <summary>
        /// 异步获取缓存项，如果不存在，则使用提供的工厂函数创建、缓存并返回该项。
        /// </summary>
        /// <typeparam name="T">项的类型。</typeparam>
        /// <param name="key">缓存键，用于唯一标识缓存中的项。</param>
        /// <param name="factory">如果缓存未命中，用于创建新项的异步工厂函数。</param>
        /// <param name="absoluteExpirationRelativeToNow">
        /// 相对于现在的绝对过期时间。如果设置，则新创建的项将在指定时间后过期。
        /// 如果同时提供了 <paramref name="slidingExpiration"/>，则 <paramref name="slidingExpiration"/> 优先。
        /// </param>
        /// <param name="slidingExpiration">
        /// 滑动过期时间。如果提供，则新创建的项的初始过期时间将设置为此值。
        /// 要实现实际的滑动行为，调用者在后续通过 <see cref="GetAsync{T}"/> 获取此项时，
        /// 需要提供相同的 TimeSpan 值给 <c>refreshSlidingExpirationWith</c> 参数。
        /// 如果此参数有值，它将优先于 <paramref name="absoluteExpirationRelativeToNow"/>。
        /// </param>
        /// <param name="refreshSlidingExpirationWith">
        /// 如果从缓存中获取到现有项，并且提供了此值，则使用此 TimeSpan 刷新键的过期时间。
        /// </param>
        /// <param name="cancellationToken">用于观察取消请求的标记。</param>
        /// <returns>表示异步操作的任务。任务结果包含缓存的项或新创建并缓存的项。</returns>
        Task<T?> GetOrCreateAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? absoluteExpirationRelativeToNow = null,
            TimeSpan? slidingExpiration = null,
            TimeSpan? refreshSlidingExpirationWith = null, // Added to pass to internal GetAsync
            CancellationToken cancellationToken = default);
    }
}