using RPG2D_Server.Infrastructure.Caching;
using RPG2D_Server.Shared.Caching;

namespace RPG2D_Server.Composition
{
    public static class RedisServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisCaching(this IServiceCollection services)
        {
            services.AddOptions<RedisCacheOptions>().BindConfiguration("Redis")
                .Validate(options => !options.Enabled || options.OperationTimeoutMilliseconds is >= 100 and <= 2000, "启用缓存时 Redis:OperationTimeoutMilliseconds 必须为 100～2000。")
                .ValidateOnStart();
            services.AddSingleton<RedisConnection>();
            services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<RedisConnection>());
            services.AddSingleton<ICacheStore, RedisCacheStore>();
            return services;
        }
    }
}