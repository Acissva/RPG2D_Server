using RPG2D_Server.Features.NpcChat.Contracts;

namespace RPG2D_Server.Features.NpcChat
{
    public interface INpcChatGateway
    {
        Task<NpcChatResponse> ChatAsync(NpcChatRequest request, CancellationToken cancellationToken);
    }
}
