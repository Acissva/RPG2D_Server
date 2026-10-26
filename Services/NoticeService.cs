using RPG2D_Server.Caching.Notice;
using RPG2D_Server.Contracts;
using RPG2D_Server.Data;
using RPG2D_Server.Validation;

namespace RPG2D_Server.Services
{
    public sealed class NoticeService : INoticeService
    {
        private readonly INoticeCache _cache;
        private readonly INoticeRepository _repository;
        private readonly ILogger<NoticeService> _logger;

        public NoticeService(INoticeRepository repository, INoticeCache cache, ILogger<NoticeService> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
        }

        //Task<T>：这个操作异步完成后，会得到一个 T；也可能失败或取消。需要配合 async 和 await 使用
        public async Task<NoticeResponse?> GetNoticeAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cached = await _cache.GetCurrentAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (cached != null)
            {
                return new NoticeResponse { ProtocolVersion = 1, Title = cached.Title, Content = cached.Content };
            }

            var notice = await _repository.GetCurrentAsync(cancellationToken);

            if (notice == null)
            {
                return null;
            }

            ValidateNotice(notice);
            _logger.LogInformation("读取数据库公告，编号 {NoticeId}，协议版本 {ProtocolVersion}。", notice.Id, 1);
            cancellationToken.ThrowIfCancellationRequested();
            await _cache.SetCurrentAsync(notice, cancellationToken);
            return new NoticeResponse { ProtocolVersion = 1, Title = notice.Title, Content = notice.Content };
        }

        public async Task<DevelopmentNoticeResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
            var notice = await _repository.GetByIdAsync(id, cancellationToken);

            if (notice is null)
            {
                return null;
            }

            ValidateNotice(notice);
            return new DevelopmentNoticeResponse { Id = notice.Id, Title = notice.Title, Content = notice.Content };
        }

        public async Task<DevelopmentNoticeResponse> CreateAsync(NoticeWriteRequest request, CancellationToken cancellationToken)
        {
            ValidateWriteRequest(request);
            var id = await ExecuteWriteAndInvalidateAsync(token => _repository.CreateAsync(request.Title!, request.Content!, token), cancellationToken);
            _logger.LogInformation("已创建公告，编号 {NoticeId}。", id);
            return new DevelopmentNoticeResponse { Id = id, Title = request.Title!, Content = request.Content! };
        }

        public Task<bool> UpdateAsync(long id, NoticeWriteRequest request, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
            ValidateWriteRequest(request);
            return ExecuteWriteAndInvalidateAsync(token => _repository.UpdateAsync(id, request.Title!, request.Content!, token), cancellationToken);
        }

        public Task<bool> DeleteAsync(long id, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
            return ExecuteWriteAndInvalidateAsync(token => _repository.DeleteAsync(id, token), cancellationToken);
        }

        private static void ValidateNotice(NoticeRecord notice)
        {
            if (notice.Id <= 0 || NoticeContentRules.GetErrors(notice.Title, notice.Content).Count > 0)
            {
                throw new InvalidOperationException("数据库中的公告不符合成功响应协议。");
            }
        }

        private static void ValidateWriteRequest(NoticeWriteRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var errors = NoticeContentRules.GetErrors(request.Title, request.Content);

            if (errors.Count > 0)
            {
                throw new InputValidationException(errors);
            }
        }

        private async Task<TResult> ExecuteWriteAndInvalidateAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                //这里执行完 operation 并拿到结果后，会先去执行 finally，执行完 finally 后再 return
                //try 中代码报错，也会先执行 finally，然后向上抛异常
                return await operation(cancellationToken);
            }
            finally
            {
                //如果 finally 中的代码存在【未捕获】的异常，则会覆盖 try 里要返回的结果（或覆盖原有的异常）
                //由于 InvalidateAfterWriteAsync 不会向上抛出异常，所以无论清理缓存是否成功，operation 返回的结果不受影响
                await InvalidateAfterWriteAsync();
            }
        }

        private async Task InvalidateAfterWriteAsync()
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            //该段 catch 捕获了所有类型的异常，所以不会向上抛出异常
            try
            {
                await _cache.InvalidateCurrentAsync(cleanup.Token);
            }
            catch (OperationCanceledException) when (cleanup.IsCancellationRequested)
            {
                _logger.LogWarning("公告写入后的缓存清理达到期限，旧值可能保留至过期。");
            }
            catch (Exception exception)
            {
                _logger.LogError("公告写入后的缓存清理发生异常，保留原数据库结果，异常类型 {ExceptionType}。", exception.GetType().Name);
            }
        }
    }
}