using Microsoft.AspNetCore.Mvc;
using RPG2D_Server.Contracts;

namespace RPG2D_Server.Controllers
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
