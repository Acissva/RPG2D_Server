using Microsoft.AspNetCore.Mvc;
using RPG2D_Server.Features.Notice.Contracts;
using RPG2D_Server.Features.Notice.Services;

namespace RPG2D_Server.Features.Notice.Controllers
{
    [ApiController]
    [Route("api/notice")]
    public sealed class NoticeController : ControllerBase
    {
        private readonly INoticeService _noticeService;

        public NoticeController(INoticeService noticeService)
        {
            _noticeService = noticeService;
        }

        //ActionResult<T>：这个 Controller 动作可以返回成功数据 T，也可以返回其他 HTTP 动作结果，例如 404
        [HttpGet]
        public async Task<ActionResult<NoticeResponse>> GetCurrentAsync(CancellationToken cancellationToken)
        {
            var notice = await _noticeService.GetCurrentAsync(cancellationToken);

            if (notice is null)
            {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "暂无公告", detail: "当前没有可显示的公告");
            }

            return Ok(notice);
        }
    }
}