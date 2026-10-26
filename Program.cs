using MySqlConnector;
using RPG2D_Server.Caching;
using RPG2D_Server.Caching.Notice;
using RPG2D_Server.Configuration;
using RPG2D_Server.Data;
using RPG2D_Server.Filters;
using RPG2D_Server.Infrastructure;
using RPG2D_Server.Infrastructure.Caching;
using RPG2D_Server.Infrastructure.Database;
using RPG2D_Server.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Rpg2D");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("缺少 ConnectionStrings:Rpg2D 数据库配置，请检查本机开发配置。");
}

MySqlConnectionStringBuilder connectionOptions;

try
{
    connectionOptions = new MySqlConnectionStringBuilder(connectionString);
}
catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
{
    throw new InvalidOperationException("ConnectionStrings:Rpg2D 连接串格式无效，请检查本机开发配置。");
}

//UseAffectedRows  false：返回匹配到的行数   true：返回实际修改的行数
if (connectionOptions.UseAffectedRows)
{
    throw new InvalidOperationException("公告更新要求 UseAffectedRows=False，以匹配行数判断记录是否存在。");
}

if (string.IsNullOrWhiteSpace(connectionOptions.Database))
{
    throw new InvalidOperationException("ConnectionStrings:Rpg2D 必须指定 Database。");
}

var cacheEnvironment = Uri.EscapeDataString(builder.Environment.EnvironmentName.ToLowerInvariant());
var cacheDatabase = Uri.EscapeDataString(connectionOptions.Database);
var noticeCacheKey = $"rpg2d:{cacheEnvironment}:{cacheDatabase}:notice:current:v1";

builder.Services.AddOptions<RedisCacheOptions>().BindConfiguration("Redis")
    .Validate(options => !options.Enabled || options.OperationTimeoutMilliseconds is >= 100 and <= 2000,
    "启用缓存时 Redis:OperationTimeoutMilliseconds 必须为 100～2000。").ValidateOnStart();
builder.Services.AddSingleton<RedisConnection>();
builder.Services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<RedisConnection>());
builder.Services.AddSingleton<ICacheStore, RedisCacheStore>();
builder.Services.AddOptions<NoticeCacheOptions>().BindConfiguration("NoticeCache")
    .Configure(options => options.CurrentKey = noticeCacheKey)
    .Validate(options => options.TtlSeconds is >= 1 and <= 300, "NoticeCache:TtlSeconds 必须为 1～300。")
    .Validate(options => !string.IsNullOrWhiteSpace(options.CurrentKey), "公告缓存 Key 未构造。").ValidateOnStart();
builder.Services.AddSingleton<INoticeCache, NoticeCache>();
builder.Services.AddMySqlDataSource(connectionOptions.ConnectionString);
builder.Services.AddSingleton<MySqlOperationExecutor>();
builder.Services.AddScoped<INoticeRepository, MySqlNoticeRepository>();
builder.Services.AddScoped<INoticeService, NoticeService>();
builder.Services.AddScoped<DevelopmentNoticeAccessFilter>();
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.Run();
