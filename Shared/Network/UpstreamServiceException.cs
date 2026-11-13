namespace RPG2D_Server.Shared.Network
{
    public sealed class UpstreamServiceException : Exception
    {
        public enum FailureKind { Unavailable, InvalidResponse, Timeout, Busy }
        public FailureKind Kind { get; }

        public UpstreamServiceException(FailureKind kind) : base("上游调用未取得有效结果。")
        {
            Kind = kind;
        }
    }
}
