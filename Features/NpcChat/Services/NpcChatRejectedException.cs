namespace RPG2D_Server.Features.NpcChat.Services
{
    public sealed class NpcChatRejectedException : Exception
    {
        public enum FailureKind { UnsupportedNpc, Busy }
        public FailureKind Kind { get; }

        public NpcChatRejectedException(FailureKind kind) : base("本次交谈无法受理。")
        {
            Kind = kind;
        }
    }
}
