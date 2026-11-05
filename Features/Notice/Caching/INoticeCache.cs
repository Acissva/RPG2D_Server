using RPG2D_Server.Features.Notice.Data;

namespace RPG2D_Server.Features.Notice.Caching
{
    public interface INoticeCache
    {
        Task<NoticeRecord?> GetCurrentAsync(CancellationToken cancellationToken);
        Task SetCurrentAsync(NoticeRecord notice, CancellationToken cancellationToken);
        Task InvalidateCurrentAsync(CancellationToken cancellationToken);
    }
}