namespace RPG2D_Server.Infrastructure.Ai
{
    public sealed class PythonAiOptions
    {
        public string BaseUrl { get; set; } = "http://127.0.0.1:8001/";
        public int RequestTimeoutSeconds { get; set; } = 55;
    }
}
