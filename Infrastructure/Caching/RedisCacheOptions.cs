namespace RPG2D_Server.Infrastructure.Caching
{
    public sealed class RedisCacheOptions
    {
        public bool Enabled { get; set; }
        public int OperationTimeoutMilliseconds { get; set; } = 500;
    }
}