using System.Text.Json.Serialization;

namespace RPG2D_Server.Features.NpcChat.Contracts
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class NpcChatResponse
    {
        [JsonNumberHandling(JsonNumberHandling.Strict)]
        public required int ProtocolVersion { get; init; }
        public required string RequestId { get; init; }
        public required string NpcId { get; init; }
        public required string Reply { get; init; }
    }
}
