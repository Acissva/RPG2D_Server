using Microsoft.Extensions.Options;
using RPG2D_Server.Features.Notice.Caching;
using RPG2D_Server.Features.Notice.Configuration;
using RPG2D_Server.Features.Notice.Data;
using RPG2D_Server.Features.Notice.Filters;
using RPG2D_Server.Features.Notice.Services;
using RPG2D_Server.Infrastructure.Caching;

namespace RPG2D_Server.Composition
{
    public static class NoticeServiceCollectionExtensions
    {
        public static IServiceCollection AddNoticeFeature(this IServiceCollection services, string environmentName, string databaseName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(environmentName);
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
            var cacheEnvironment = Uri.EscapeDataString(environmentName.ToLowerInvariant());
            var cacheDatabase = Uri.EscapeDataString(databaseName);
            var noticeCacheKey = $"rpg2d:{cacheEnvironment}:{cacheDatabase}:notice:current:v1";

            services.AddOptions<NoticeCacheOptions>().BindConfiguration("NoticeCache")
                .Configure(options => options.CurrentKey = noticeCacheKey)
                .Validate(options => options.TtlSeconds is >= 1 and <= 300, "NoticeCache:TtlSeconds 必须为 1～300。")
                .Validate(options => !string.IsNullOrWhiteSpace(options.CurrentKey), "公告缓存 Key 未构造。")
                .ValidateOnStart();
            services.AddSingleton<INoticeCache>(serviceProvider =>
            {
                var redisOptions = serviceProvider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;

                if (!redisOptions.Enabled)
                {
                    return new DisabledNoticeCache();
                }

                return ActivatorUtilities.CreateInstance<NoticeCache>(serviceProvider);
            });
            services.AddScoped<INoticeRepository, MySqlNoticeRepository>();
            services.AddScoped<INoticeService, NoticeService>();
            services.AddScoped<DevelopmentNoticeAccessFilter>();
            return services;
        }
    }
}