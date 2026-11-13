using RPG2D_Server.Features.NpcChat.Contracts;
using System.Text;

namespace RPG2D_Server.Features.NpcChat.Validation
{
    public static class NpcChatContentRules
    {
        public const string GuideNpcId = "npc_ai_guide_001";
        private static readonly UnicodeEncoding StrictUtf16 = new(false, false, true);

        public static bool IsValidText(string? value, int maximum)
        {
            if (value is null || value.Length == 0 || value.Length > maximum)
            {
                return false;
            }

            try
            {
                StrictUtf16.GetByteCount(value);
            }
            catch (EncoderFallbackException)
            {
                return false;
            }

            return value.Any(character => !char.IsWhiteSpace(character) && character is not (>= '\u001c' and <= '\u001f'));
        }

        public static Dictionary<string, string[]> GetErrors(NpcChatRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            var errors = new Dictionary<string, string[]>();
            if (request.ProtocolVersion != 1) errors["protocolVersion"] = new[] { "协议版本必须为 1。" };
            if (request.RequestId is null || request.RequestId.Length != 32 || request.RequestId.Any(character => !Uri.IsHexDigit(character))) errors["requestId"] = new[] { "关联编号必须为 32 位十六进制。" };
            if (string.IsNullOrEmpty(request.NpcId)) errors["npcId"] = new[] { "必须指定 NPC。" };
            if (!IsValidText(request.Message, 256)) errors["message"] = new[] { "输入必须为 1～256 个有效 UTF-16 代码单元，且非全空白。" };
            if (request.History is null || request.History.Length > 4)
            {
                errors["history"] = new[] { "历史必须为最多四个完整问答对。" };
                return errors;
            }

            for (var index = 0; index < request.History.Length; index++)
            {
                var turn = request.History[index];
                if (turn is null || !IsValidText(turn.PlayerText, 256) || !IsValidText(turn.NpcText, 1024)) errors[$"history[{index}]"] = new[] { "历史问答缺失、空白或超出有效文本上限。" };
            }

            return errors;
        }
    }
}
