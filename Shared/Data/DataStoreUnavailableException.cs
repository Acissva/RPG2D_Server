namespace RPG2D_Server.Shared.Data
{
    public sealed class DataStoreUnavailableException : Exception
    {
        public DataStoreUnavailableException() : base("数据存储暂时不可用。")
        {

        }
    }
}
