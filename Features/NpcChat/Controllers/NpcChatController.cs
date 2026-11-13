using Microsoft.AspNetCore.Mvc;
using RPG2D_Server.Features.NpcChat.Contracts;
using RPG2D_Server.Features.NpcChat.Services;
using RPG2D_Server.Infrastructure.Web.Filters;

namespace RPG2D_Server.Features.NpcChat.Controllers
{
    [ApiController]
    [Route("api/npc-chat")]
    public sealed class NpcChatController : ControllerBase
    {
        private readonly INpcChatService _service;

        public NpcChatController(INpcChatService service)
        {
            _service = service;
        }

        [HttpPost]
        [Utf8Json]
        [RequestSizeLimit(64 * 1024)]
        public async Task<ActionResult<NpcChatResponse>> ChatAsync([FromBody] NpcChatRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(await _service.ChatAsync(request, cancellationToken));
            }
            catch (NpcChatRejectedException exception)
            {
                return exception.Kind == NpcChatRejectedException.FailureKind.UnsupportedNpc ? Problem(statusCode: StatusCodes.Status404NotFound, title: "该 NPC 不支持 AI 交谈。") : Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "交谈服务忙碌，请稍后重试。");
            }
        }
    }
}
