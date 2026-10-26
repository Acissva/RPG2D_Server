using RPG2D_Server.Data;

namespace RPG2D_Server.Caching.Notice
{
    public interface INoticeCache
    {
        Task<NoticeRecord?> GetCurrentAsync(CancellationToken cancellationToken);
        Task SetCurrentAsync(NoticeRecord notice, CancellationToken cancellationToken);
        Task InvalidateCurrentAsync(CancellationToken cancellationToken);
    }
}