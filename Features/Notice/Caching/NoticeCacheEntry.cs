namespace RPG2D_Server.Features.Notice.Caching
{
    public sealed record NoticeCacheEntry(int SchemaVersion, long Id, string? Title, string? Content);
}