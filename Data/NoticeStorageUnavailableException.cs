namespace RPG2D_Server.Data
{
    public sealed class NoticeStorageUnavailableException : Exception
    {
        public NoticeStorageUnavailableException() : base("公告数据存储暂时不可用。")
        {

        }
    }
}
