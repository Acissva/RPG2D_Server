namespace RPG2D_Server.Features.Health
{
    public sealed class HealthResponse
    {
        public string Status { get; set; } = string.Empty;

        public string Service { get; set; } = string.Empty;

        public int ProtocolVersion { get; set; }
    }
}