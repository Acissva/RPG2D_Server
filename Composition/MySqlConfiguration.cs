using MySqlConnector;

namespace RPG2D_Server.Composition
{
    public static class MySqlConfiguration
    {
        public static MySqlConnectionStringBuilder ReadValidatedOptions(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("Rpg2D");

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

            if (connectionOptions.UseAffectedRows)
            {
                throw new InvalidOperationException("公告更新要求 UseAffectedRows=False，以匹配行数判断记录是否存在。");
            }

            if (string.IsNullOrWhiteSpace(connectionOptions.Database))
            {
                throw new InvalidOperationException("ConnectionStrings:Rpg2D 必须指定 Database。");
            }

            return connectionOptions;
        }
    }
}