namespace RPG2D_Server.Validation
{
    public sealed class InputValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public InputValidationException(Dictionary<string, string[]> errors) : base("输入数据无效。")
        {
            Errors = new Dictionary<string, string[]>(errors);
        }
    }
}