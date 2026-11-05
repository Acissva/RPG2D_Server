using Microsoft.AspNetCore.Mvc;

namespace RPG2D_Server.Features.Health
{
    [ApiController]
    [Route("api/health")]
    public sealed class HealthController : ControllerBase
    {
        [HttpGet]
        public ActionResult<HealthResponse> Get()
        {
            return Ok(new HealthResponse { Status = "ok", Service = "RPG2D_Server", ProtocolVersion = 1 });
        }
    }
}
