using MySqlConnector;

namespace RPG2D_Server.Data
{
    public sealed class MySqlNoticeRepository : INoticeRepository
    {
        private readonly MySqlDataSource _dataSource;
        private readonly ILogger<MySqlNoticeRepository> _logger;

        public MySqlNoticeRepository(MySqlDataSource dataSource, ILogger<MySqlNoticeRepository> logger)
        {
            _dataSource = dataSource;
            _logger = logger;
        }

        public Task<NoticeRecord?> GetCurrentAsync(CancellationToken cancellationToken)
        {
            return ReadSingleAsync("SELECT id, title, content FROM notices ORDER BY id DESC LIMIT 1", null, cancellationToken);
        }

        public Task<NoticeRecord?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            return ReadSingleAsync("SELECT id, title, content FROM notices WHERE id = @id", id, cancellationToken);
        }

        public Task<long> CreateAsync(string title, string content, CancellationToken cancellationToken)
        {
            return ExecuteAsync(async (connection, token) =>
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO notices (title, content) VALUES (@title, @content);";
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@content", content);
                var affectedRows = await command.ExecuteNonQueryAsync(token);

                if (affectedRows != 1 || command.LastInsertedId <= 0)
                {
                    throw new InvalidOperationException("新增公告未返回预期的单行结果与编号。");
                }

                return command.LastInsertedId;
            }, cancellationToken);
        }

        public Task<bool> UpdateAsync(long id, string title, string content, CancellationToken cancellationToken)
        {
            return ExecuteAsync(async (connection, token) =>
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "UPDATE notices SET title = @title, content = @content WHERE id = @id;";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@title", title);
                command.Parameters.AddWithValue("@content", content);
                return WasSingleRowMatched(await command.ExecuteNonQueryAsync(token));
            }, cancellationToken);
        }

        public Task<bool> DeleteAsync(long id, CancellationToken cancellationToken)
        {
            return ExecuteAsync(async (connection, token) =>
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM notices WHERE id = @id;";
                command.Parameters.AddWithValue("@id", id);
                return WasSingleRowMatched(await command.ExecuteNonQueryAsync(token));
            }, cancellationToken);
        }

        private async Task<TResult> ExecuteAsync<TResult>(Func<MySqlConnection, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(3));

            try
            {
                await using var connection = await _dataSource.OpenConnectionAsync(deadline.Token);
                return await operation(connection, deadline.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
            {
                _logger.LogWarning("公告数据库操作达到访问期限。");
                throw new NoticeStorageUnavailableException();
            }
            catch (MySqlException exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogError("公告数据库操作失败，数据库错误码 {DatabaseErrorCode}。", exception.Number);

                if (deadline.IsCancellationRequested || exception.IsTransient
                    || exception.ErrorCode is MySqlErrorCode.UnableToConnectToHost
                    or MySqlErrorCode.CommandTimeoutExpired
                    or MySqlErrorCode.AccessDenied
                    or MySqlErrorCode.DatabaseAccessDenied
                    or MySqlErrorCode.TableAccessDenied)
                {
                    throw new NoticeStorageUnavailableException();
                }

                throw;
            }
        }

        private Task<NoticeRecord?> ReadSingleAsync(string sql, long? id, CancellationToken cancellationToken)
        {
            return ExecuteAsync<NoticeRecord?>(async (connection, token) =>
            {
                await using var command = connection.CreateCommand();
                command.CommandText = sql;

                if (id.HasValue)
                {
                    command.Parameters.AddWithValue("@id", id.Value);
                }

                await using var reader = await command.ExecuteReaderAsync(token);

                if (!await reader.ReadAsync(token))
                {
                    return null;
                }

                return new NoticeRecord(reader.GetInt64(0), reader.GetString(1), reader.GetString(2));
            }, cancellationToken);
        }

        private static bool WasSingleRowMatched(int affectedRows)
        {
            if (affectedRows < 0 || affectedRows > 1)
            {
                throw new InvalidOperationException("按主键写入公告返回了非预期行数。");
            }

            return affectedRows == 1;
        }
    }
}
