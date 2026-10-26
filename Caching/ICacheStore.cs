namespace RPG2D_Server.Caching
{
    public interface ICacheStore
    {
        Task<string?> GetStringAsync(string key, CancellationToken cancellationToken);
        Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken);
        Task DeleteAsync(string key, CancellationToken cancellationToken);
    }
}