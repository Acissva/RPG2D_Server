using Microsoft.Extensions.Options;
using RPG2D_Server.Caching;
using RPG2D_Server.Configuration;
using StackExchange.Redis;

namespace RPG2D_Server.Infrastructure.Caching
{
    public sealed class RedisCacheStore : ICacheStore
    {
        private readonly RedisConnection _connection;
        private readonly RedisCacheOptions _options;
        private readonly ILogger<RedisCacheStore> _logger;

        public RedisCacheStore(RedisConnection connection, IOptions<RedisCacheOptions> options, ILogger<RedisCacheStore> logger)
        {
            _connection = connection;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            var value = await ExecuteAsync(database => database.StringGetAsync(key), cancellationToken);
            return value.IsNull ? null : value.ToString();
        }

        public async Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(value);

            if (ttl < TimeSpan.FromMilliseconds(1))
            {
                throw new ArgumentOutOfRangeException(nameof(ttl), "缓存有效期不能小于 1 毫秒。");
            }

            var written = await ExecuteAsync(database => database.StringSetAsync(key, value, new Expiration(ttl)), cancellationToken);

            if (!written)
            {
                throw new InvalidOperationException("Redis 未确认无条件字符串写入成功。");
            }
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            await ExecuteAsync(database => database.KeyDeleteAsync(key), cancellationToken);
        }

        private async Task<TResult> ExecuteAsync<TResult>(Func<IDatabase, Task<TResult>> operation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_options.Enabled)
            {
                throw new CacheUnavailableException();
            }

            Task<TResult>? pendingTask = null;

            try
            {
                pendingTask = operation(_connection.GetDatabase());
                return await pendingTask.WaitAsync(TimeSpan.FromMilliseconds(_options.OperationTimeoutMilliseconds), cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _ = ObserveCompletionAsync(pendingTask);
                throw;
            }
            catch (Exception exception) when (exception is RedisConnectionException or RedisTimeoutException or TimeoutException)
            {
                _ = ObserveCompletionAsync(pendingTask);
                cancellationToken.ThrowIfCancellationRequested();
                throw new CacheUnavailableException();
            }
        }

        private async Task ObserveCompletionAsync(Task? pendingTask)
        {
            if (pendingTask == null)
            {
                return;
            }

            try
            {
                await pendingTask;
            }
            catch (Exception exception)
            {
                _logger.LogDebug("Redis 命令最终失败，异常类型 {ExceptionType}。", exception.GetType().Name);
            }
        }
    }
}