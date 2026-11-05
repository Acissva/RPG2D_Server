using RPG2D_Server.Features.Notice.Data;
using RPG2D_Server.Features.Notice.Validation;

namespace RPG2D_Server.Features.Notice.Caching
{
    public sealed class DisabledNoticeCache : INoticeCache
    {
        public Task<NoticeRecord?> GetCurrentAsync(CancellationToken cancellationToken)
        {
            // Task.FromXXX : 快速创建一个已经完成状态的 Task 的静态工厂方法，用于异步编程场景，避免不必要的 `async/await` 包装
            return cancellationToken.IsCancellationRequested ? Task.FromCanceled<NoticeRecord?>(cancellationToken) : Task.FromResult<NoticeRecord?>(null);
        }

        public Task SetCurrentAsync(NoticeRecord notice, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (notice is null)
            {
                return Task.FromException(new ArgumentNullException(nameof(notice)));
            }

            if (notice.Id <= 0 || NoticeContentRules.GetErrors(notice.Title, notice.Content).Count > 0)
            {
                return Task.FromException(new InvalidOperationException("不能缓存无效公告。"));
            }

            return Task.CompletedTask;
        }

        public Task InvalidateCurrentAsync(CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
        }
    }
}