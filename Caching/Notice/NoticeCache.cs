using Microsoft.Extensions.Options;
using RPG2D_Server.Configuration;
using RPG2D_Server.Data;
using RPG2D_Server.Validation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RPG2D_Server.Caching.Notice
{
    public sealed class NoticeCache : INoticeCache
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        private readonly ICacheStore _store;
        private readonly RedisCacheOptions _redisOptions;
        private readonly NoticeCacheOptions _options;
        private readonly ILogger<NoticeCache> _logger;

        public NoticeCache(ICacheStore store, IOptions<RedisCacheOptions> redisOptions, IOptions<NoticeCacheOptions> options, ILogger<NoticeCache> logger)
        {
            _store = store;
            _redisOptions = redisOptions.Value;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<NoticeRecord?> GetCurrentAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_redisOptions.Enabled)
            {
                return null;
            }

            string? json;

            try
            {
                json = await _store.GetStringAsync(_options.CurrentKey, cancellationToken);
            }
            catch (CacheUnavailableException)
            {
                _logger.LogWarning("公告缓存读取不可用，回源数据库。");
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (json == null)
            {
                _logger.LogInformation("公告缓存未命中。");
                return null;
            }

            NoticeCacheEntry? entry;

            try
            {
                entry = JsonSerializer.Deserialize<NoticeCacheEntry>(json, JsonOptions);
            }
            catch (JsonException)
            {
                _logger.LogWarning("公告缓存 JSON 无效，将尝试失效。");
                await InvalidateCurrentAsync(cancellationToken);
                return null;
            }

            if (entry == null || entry.SchemaVersion != 1 || entry.Id <= 0 || NoticeContentRules.GetErrors(entry.Title, entry.Content).Count > 0)
            {
                _logger.LogWarning("公告缓存格式版本或内容无效，将尝试失效。");
                await InvalidateCurrentAsync(cancellationToken);
                return null;
            }

            _logger.LogInformation("命中 Redis 公告缓存，编号 {NoticeId}。", entry.Id);
            return new NoticeRecord(entry.Id, entry.Title!, entry.Content!);
        }

        public async Task SetCurrentAsync(NoticeRecord notice, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(notice);

            if (notice.Id <= 0 || NoticeContentRules.GetErrors(notice.Title, notice.Content).Count > 0)
            {
                throw new InvalidOperationException("不能缓存无效公告。");
            }

            if (!_redisOptions.Enabled)
            {
                return;
            }

            var entry = new NoticeCacheEntry(1, notice.Id, notice.Title, notice.Content);
            var json = JsonSerializer.Serialize(entry, JsonOptions);

            try
            {
                await _store.SetStringAsync(_options.CurrentKey, json, TimeSpan.FromSeconds(_options.TtlSeconds), cancellationToken);
                _logger.LogInformation("回填 Redis 公告缓存，编号 {NoticeId}，TTL {TtlSeconds} 秒。", notice.Id, _options.TtlSeconds);
            }
            catch (CacheUnavailableException)
            {
                _logger.LogWarning("公告缓存回填不可用，保留数据库读取结果。");
            }
        }

        public async Task InvalidateCurrentAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_redisOptions.Enabled)
            {
                return;
            }

            try
            {
                await _store.DeleteAsync(_options.CurrentKey, cancellationToken);
                _logger.LogInformation("已使当前公告缓存失效。");
            }
            catch (CacheUnavailableException)
            {
                _logger.LogWarning("公告缓存失效失败，旧值可能保留至过期。");
            }
        }
    }
}