using Microsoft.AspNetCore.Mvc;
using RPG2D_Server.Contracts;
using RPG2D_Server.Services;

namespace RPG2D_Server.Controllers
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
        public async Task<ActionResult<NoticeResponse>> Get(CancellationToken cancellationToken)
        {
            var notice = await _noticeService.GetNoticeAsync(cancellationToken);

            if (notice is null)
            {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "暂无公告", detail: "当前没有可显示的公告");
            }

            return Ok(notice);
        }
    }
}