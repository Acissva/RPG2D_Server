namespace RPG2D_Server.Contracts
{
    public sealed class NoticeResponse
    {
        public int ProtocolVersion { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;
    }
}
