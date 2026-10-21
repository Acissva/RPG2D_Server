namespace RPG2D_Server.Validation
{
    public sealed class NoticeValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public NoticeValidationException(Dictionary<string, string[]> errors) : base("公告输入无效。")
        {
            Errors = new Dictionary<string, string[]>(errors);
        }
    }
}
