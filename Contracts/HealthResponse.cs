namespace RPG2D_Server.Contracts
{
    public sealed class HealthResponse
    {
        public string Status { get; set; } = string.Empty;

        public string Service { get; set; } = string.Empty;

        public int ProtocolVersion { get; set; }
    }
}