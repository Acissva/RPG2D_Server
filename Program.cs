using MySqlConnector;
using RPG2D_Server.Data;
using RPG2D_Server.Filters;
using RPG2D_Server.Infrastructure;
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

builder.Services.AddMySqlDataSource(connectionOptions.ConnectionString);
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
