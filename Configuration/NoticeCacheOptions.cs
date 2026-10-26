namespace RPG2D_Server.Configuration
{
    public sealed class NoticeCacheOptions
    {
        public int TtlSeconds { get; set; } = 30;
        public string CurrentKey { get; set; } = string.Empty;
    }
}