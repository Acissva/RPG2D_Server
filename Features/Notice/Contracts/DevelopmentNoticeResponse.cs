namespace RPG2D_Server.Features.Notice.Contracts
{
    public class DevelopmentNoticeResponse
    {
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
