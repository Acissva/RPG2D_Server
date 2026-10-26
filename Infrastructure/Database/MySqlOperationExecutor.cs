using MySqlConnector;
using RPG2D_Server.Data;

namespace RPG2D_Server.Infrastructure.Database
{
    public sealed class MySqlOperationExecutor
    {
        private readonly MySqlDataSource _dataSource;
        private readonly ILogger<MySqlOperationExecutor> _logger;

        public MySqlOperationExecutor(MySqlDataSource dataSource, ILogger<MySqlOperationExecutor> logger)
        {
            _dataSource = dataSource;
            _logger = logger;
        }

        public async Task<TResult> ExecuteAsync<TResult>(Func<MySqlConnection, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(operation);
            cancellationToken.ThrowIfCancellationRequested();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(3));

            try
            {
                await using var connection = await _dataSource.OpenConnectionAsync(deadline.Token);
                return await operation(connection, deadline.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
            {
                _logger.LogWarning("MySQL 数据库操作达到访问期限。");
                throw new DataStoreUnavailableException();
            }
            catch (MySqlException exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogError("MySQL 数据库操作失败，数据库错误码 {DatabaseErrorCode}。", exception.Number);

                if (deadline.IsCancellationRequested || exception.IsTransient
                    || exception.ErrorCode is MySqlErrorCode.UnableToConnectToHost
                    or MySqlErrorCode.CommandTimeoutExpired
                    or MySqlErrorCode.AccessDenied
                    or MySqlErrorCode.DatabaseAccessDenied
                    or MySqlErrorCode.TableAccessDenied)
                {
                    throw new DataStoreUnavailableException();
                }

                throw;
            }
        }
    }
}
