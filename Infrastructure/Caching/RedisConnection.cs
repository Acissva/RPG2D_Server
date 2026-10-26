using Microsoft.Extensions.Options;
using RPG2D_Server.Configuration;
using StackExchange.Redis;

namespace RPG2D_Server.Infrastructure.Caching
{
    public sealed class RedisConnection : IHostedService, IDisposable
    {
        private readonly RedisCacheOptions _options;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RedisConnection> _logger;
        private ConnectionMultiplexer? _connection;

        public RedisConnection(IOptions<RedisCacheOptions> options, IConfiguration configuration, ILogger<RedisConnection> logger)
        {
            _options = options.Value;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_options.Enabled)
            {
                _logger.LogInformation("Redis 缓存已禁用，不创建连接。");
                return;
            }

            var connectionString = _configuration.GetConnectionString("Redis");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("启用缓存时必须配置 ConnectionStrings:Redis。");
            }

            ConfigurationOptions connectionOptions;

            try
            {
                connectionOptions = ConfigurationOptions.Parse(connectionString);
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
            {
                throw new InvalidOperationException("ConnectionStrings:Redis 连接串格式无效，请检查本机配置。");
            }

            if (connectionOptions.EndPoints.Count == 0)
            {
                throw new InvalidOperationException("ConnectionStrings:Redis 必须包含服务地址。");
            }

            connectionOptions.AbortOnConnectFail = false;
            connectionOptions.ConnectTimeout = 1000;
            connectionOptions.ConnectRetry = 1;
            connectionOptions.AsyncTimeout = _options.OperationTimeoutMilliseconds;
            connectionOptions.HeartbeatInterval = TimeSpan.FromMilliseconds(100);
            connectionOptions.BacklogPolicy = BacklogPolicy.FailFast;
            connectionOptions.ClientName = "RPG2D_Server";
            connectionOptions.AllowAdmin = false;
            _connection = await ConnectionMultiplexer.ConnectAsync(connectionOptions);

            if (cancellationToken.IsCancellationRequested)
            {
                Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }

            _logger.LogInformation("Redis 共享连接已初始化，当前已连接：{IsConnected}。", _connection.IsConnected);
        }

        public IDatabase GetDatabase()
        {
            return (_connection ?? throw new InvalidOperationException("Redis 连接尚未初始化、已禁用或已释放。")).GetDatabase();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _connection = null;
        }
    }
}