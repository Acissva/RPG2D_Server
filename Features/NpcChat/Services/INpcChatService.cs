using RPG2D_Server.Features.NpcChat.Contracts;

namespace RPG2D_Server.Features.NpcChat.Services
{
    public interface INpcChatService
    {
        Task<NpcChatResponse> ChatAsync(NpcChatRequest request, CancellationToken cancellationToken);
    }
}
