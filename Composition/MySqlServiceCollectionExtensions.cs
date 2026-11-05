using MySqlConnector;
using RPG2D_Server.Infrastructure.Database;

namespace RPG2D_Server.Composition
{
    public static class MySqlServiceCollectionExtensions
    {
        public static IServiceCollection AddMySqlDatabase(this IServiceCollection services, string connectionString)
        {
            services.AddMySqlDataSource(connectionString);
            services.AddSingleton<MySqlOperationExecutor>();
            return services;
        }
    }
}