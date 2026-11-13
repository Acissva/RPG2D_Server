using RPG2D_Server.Features.NpcChat.Validation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RPG2D_Server.Features.NpcChat.Contracts
{
    // 严格遵守 JSON 规范，数字必须是 JSON Number 类型，不接受带引号的字符串数字；序列化输出也一定输出无引号原生数字
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed class NpcChatRequest : IValidatableObject
    {
        [JsonNumberHandling(JsonNumberHandling.Strict)]
        public required int ProtocolVersion { get; init; }
        public required string? RequestId { get; init; }
        public required string? NpcId { get; init; }
        public required string? Message { get; init; }
        public required NpcChatTurn?[]? History { get; init; }  // 两个？分别表示“数组内元素可为null”和“数组本身可为null”

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            foreach (var error in NpcChatContentRules.GetErrors(this))
            {
                yield return new ValidationResult(error.Value[0], new[] { error.Key });
            }
        }
    }
}
