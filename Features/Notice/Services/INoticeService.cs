using RPG2D_Server.Features.Notice.Contracts;

namespace RPG2D_Server.Features.Notice.Services
{
    public interface INoticeService
    {
        Task<NoticeResponse?> GetCurrentAsync(CancellationToken cancellationToken);
        Task<DevelopmentNoticeResponse?> GetByIdAsync(long id, CancellationToken cancellationToken);
        Task<DevelopmentNoticeResponse> CreateAsync(NoticeWriteRequest request, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(long id, NoticeWriteRequest request, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(long id, CancellationToken cancellationToken);
    }
}