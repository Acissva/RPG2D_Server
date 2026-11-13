using RPG2D_Server.Composition;

var builder = WebApplication.CreateBuilder(args);
var connectionOptions = MySqlConfiguration.ReadValidatedOptions(builder.Configuration);

builder.Services.AddMySqlDatabase(connectionOptions.ConnectionString);
builder.Services.AddRedisCaching();
builder.Services.AddNoticeFeature(builder.Environment.EnvironmentName, connectionOptions.Database);
builder.Services.AddNpcChatFeature();
builder.Services.AddWebApi();

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();
app.Run();