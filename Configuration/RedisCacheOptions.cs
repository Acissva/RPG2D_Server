namespace RPG2D_Server.Configuration
{
    public sealed class RedisCacheOptions
    {
        public bool Enabled { get; set; }
        public int OperationTimeoutMilliseconds { get; set; } = 500;
    }
}