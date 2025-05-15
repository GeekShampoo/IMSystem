using IMSystem.Server.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent; // Added for ConcurrentDictionary
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IMSystem.Server.Infrastructure.Caching
{
    /// <summary>
    /// 使用 Redis 实现的缓存服务。
    /// </summary>
    public class RedisCachingService : ICachingService
    {
        private readonly IDatabase _redisDatabase;
        private readonly ILogger<RedisCachingService> _logger;
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// 初始化 <see cref="RedisCachingService"/> 类的新实例。
        /// </summary>
        /// <param name="redisConnectionMultiplexer">Redis 连接多路复用器。</param>
        /// <param name="logger">日志记录器。</param>
        public RedisCachingService(IConnectionMultiplexer redisConnectionMultiplexer, ILogger<RedisCachingService> logger)
        {
            _redisDatabase = redisConnectionMultiplexer?.GetDatabase() ?? throw new ArgumentNullException(nameof(redisConnectionMultiplexer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<(bool Found, T? Value)> GetAsync<T>(string key, TimeSpan? refreshSlidingExpirationWith = null, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var redisValue = await _redisDatabase.StringGetAsync(key);
                if (!redisValue.HasValue) // Changed from IsNullOrEmpty for better clarity with RedisValue
                {
                    return (false, default);
                }

                T? deserializedValue = JsonSerializer.Deserialize<T>(redisValue.ToString());

                if (refreshSlidingExpirationWith.HasValue && deserializedValue != null)
                {
                    try
                    {
                        // Fire and forget is acceptable here as it's an optimization
                        await _redisDatabase.KeyExpireAsync(key, refreshSlidingExpirationWith.Value, CommandFlags.FireAndForget);
                        _logger.LogDebug("Cache key {Key} expiration refreshed (sliding) to {SlidingExpiration}", key, refreshSlidingExpirationWith.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to refresh sliding expiration for key {Key}", key);
                        // Continue, as the main value was still retrieved.
                    }
                }
                return (true, deserializedValue);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("GetAsync operation for key {Key} was cancelled.", key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从 Redis 获取键 {Key} 的值时出错。", key);
                return (false, default); // 根据策略静默失败或重新抛出异常
            }
        }

        /// <inheritdoc/>
        public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpirationRelativeToNow = null, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default)
        {
            TimeSpan? expiry = null;
            if (slidingExpiration.HasValue)
            {
                expiry = slidingExpiration.Value;
                if (absoluteExpirationRelativeToNow.HasValue)
                {
                    _logger.LogInformation("Both slidingExpiration and absoluteExpirationRelativeToNow were provided for key {Key}. SlidingExpiration will be used for initial TTL.", key);
                }
            }
            else if (absoluteExpirationRelativeToNow.HasValue)
            {
                expiry = absoluteExpirationRelativeToNow.Value;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var stringValue = JsonSerializer.Serialize(value);
                await _redisDatabase.StringSetAsync(key, stringValue, expiry);
                _logger.LogDebug("Cache key {Key} set with expiry {Expiry}", key, expiry?.ToString() ?? "none");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SetAsync operation for key {Key} was cancelled.", key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "向 Redis 设置键 {Key} 的值时出错。", key);
                // 静默失败或重新抛出异常
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _redisDatabase.KeyDeleteAsync(key);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("RemoveAsync operation for key {Key} was cancelled.", key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从 Redis 删除键 {Key} 时出错。", key);
            }
        }

        /// <inheritdoc/>
        public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
        {
            // Note: The primary mechanism for sliding expiration is now handled in GetAsync.
            // This method's behavior with KeyTouchAsync might be different from a typical "Refresh TTL".
            // KeyTouchAsync updates the last accessed time, used by some Redis eviction policies (like LFU/LRU with volatile-lfu/volatile-lru).
            // It does NOT reset the TTL of a key set with an absolute expiry or a specific TTL from SETEX.
            // If the key was set with a sliding expiration (via SetAsync's slidingExpiration param),
            // and GetAsync is used with refreshSlidingExpirationWith, that's the more direct way to "slide" it.
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool touched = await _redisDatabase.KeyTouchAsync(key);
                if (touched)
                {
                    _logger.LogDebug("Key {Key} was touched (if applicable by server policy).", key);
                }
                else
                {
                    _logger.LogDebug("Key {Key} not found or not touched.", key);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("RefreshAsync operation for key {Key} was cancelled.", key);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "在 Redis 中刷新键 {Key} 时出错 (using KeyTouchAsync).", key);
            }
        }

        /// <inheritdoc/>
        public async Task<T?> GetOrCreateAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? absoluteExpirationRelativeToNow = null,
            TimeSpan? slidingExpiration = null,
            TimeSpan? refreshSlidingExpirationWith = null, // Passed to internal GetAsync
            CancellationToken cancellationToken = default)
        {
            var (found, cachedValue) = await GetAsync<T>(key, refreshSlidingExpirationWith, cancellationToken);
            if (found && cachedValue != null && !cachedValue.Equals(default(T)))
            {
                return cachedValue;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var keySpecificLock = _keyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            await keySpecificLock.WaitAsync(cancellationToken);
            try
            {
                // Double-check if the value was populated by another thread while waiting for the lock
                (found, cachedValue) = await GetAsync<T>(key, refreshSlidingExpirationWith, cancellationToken);
                if (found && cachedValue != null && !cachedValue.Equals(default(T)))
                {
                    return cachedValue;
                }

                cancellationToken.ThrowIfCancellationRequested(); // Check again after acquiring lock

                var newValue = await factory();
                if (newValue != null)
                {
                    await SetAsync(key, newValue, absoluteExpirationRelativeToNow, slidingExpiration, cancellationToken);
                }
                return newValue;
            }
            finally
            {
                keySpecificLock.Release();
                // Consider removing the lock from the dictionary if its count is 0 and no one is waiting,
                // to prevent the dictionary from growing indefinitely. This adds complexity.
                // For simplicity, this example does not include automatic cleanup.
                // If keySpecificLock.CurrentCount == 1 (meaning no other waiters after release)
                // and a certain condition is met (e.g., lock not used for a while),
                // then _keyLocks.TryRemove(key, out _);
            }
        }
    }
}