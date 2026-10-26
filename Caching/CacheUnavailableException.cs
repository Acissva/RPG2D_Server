namespace RPG2D_Server.Caching
{
    public sealed class CacheUnavailableException : Exception
    {
        public CacheUnavailableException() : base("缓存服务暂时不可用。")
        {
        }
    }
}