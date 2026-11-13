using System.Text.Json.Serialization;

namespace RPG2D_Server.Features.NpcChat.Contracts
{
    // 反序列化 JSON 的时候，出现了这个类没有定义的字段（多余字段）的处理方式
    // Disallow : 抛异常
    // Skip : 忽略 (默认)
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class NpcChatTurn
    {
        // required : 初始化时必须赋值
        public required string? PlayerText { get; set; }
        public required string? NpcText { get; set; }
    }
}
