using RPG2D_Server.Contracts;
using RPG2D_Server.Data;
using RPG2D_Server.Validation;

namespace RPG2D_Server.Services
{
    public sealed class NoticeService : INoticeService
    {
        private readonly INoticeRepository _repository;
        private readonly ILogger<NoticeService> _logger;

        public NoticeService(INoticeRepository repository, ILogger<NoticeService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        //Task<T>：这个操作异步完成后，会得到一个 T；也可能失败或取消。需要配合 async 和 await 使用
        public async Task<NoticeResponse?> GetNoticeAsync(CancellationToken cancellationToken)
        {
            var notice = await _repository.GetCurrentAsync(cancellationToken);

            if (notice is null)
            {
                return null;
            }

            ValidateNotice(notice);

            _logger.LogInformation("读取数据库公告，编号 {NoticeId}，协议版本 {ProtocolVersion}。", notice.id, 1);
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
            return new DevelopmentNoticeResponse { Id = notice.id, Title = notice.Title, Content = notice.Content };
        }

        public async Task<DevelopmentNoticeResponse> CreateAsync(NoticeWriteRequest request, CancellationToken cancellationToken)
        {
            ValidateWriteRequest(request);
            var id = await _repository.CreateAsync(request.Title!, request.Content!, cancellationToken);
            _logger.LogInformation("已创建公告，编号 {NoticeId}。", id);
            return new DevelopmentNoticeResponse { Id = id, Title = request.Title!, Content = request.Content! };
        }

        public async Task<bool> UpdateAsync(long id, NoticeWriteRequest request, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
            ValidateWriteRequest(request);
            return await _repository.UpdateAsync(id, request.Title!, request.Content!, cancellationToken);
        }

        public Task<bool> DeleteAsync(long id, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
            return _repository.DeleteAsync(id, cancellationToken);
        }

        private static void ValidateNotice(NoticeRecord notice)
        {
            if (notice.id <= 0 || NoticeContentRules.GetErrors(notice.Title, notice.Content).Count > 0)
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
                throw new NoticeValidationException(errors);
            }
        }
    }
}