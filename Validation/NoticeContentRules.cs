namespace RPG2D_Server.Validation
{
    public static class NoticeContentRules
    {
        public static Dictionary<string, string[]> GetErrors(string? title, string? content)
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(title) || title.Length > 64)
            {
                errors["title"] = new[] { "公告标题必须为 1～64 个 UTF-16 代码单元，且不能只含空白。" };
            }

            if (string.IsNullOrWhiteSpace(content) || content.Length > 512)
            {
                errors["content"] = new[] { "公告正文必须为 1～512 个 UTF-16 代码单元，且不能只含空白。" };
            }

            return errors;
        }
    }
}
