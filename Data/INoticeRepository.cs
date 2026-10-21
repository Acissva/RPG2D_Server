namespace RPG2D_Server.Data
{
    public interface INoticeRepository
    {
        Task<NoticeRecord?> GetCurrentAsync(CancellationToken cancellationToken);
        Task<NoticeRecord?> GetByIdAsync(long id, CancellationToken cancellationToken);
        Task<long> CreateAsync(string title, string content, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(long id, string title, string content, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken);
    }
}
