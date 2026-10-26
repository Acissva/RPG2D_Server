namespace RPG2D_Server.Caching.Notice
{
    public sealed record NoticeCacheEntry(int SchemaVersion, long Id, string? Title, string? Content);
}