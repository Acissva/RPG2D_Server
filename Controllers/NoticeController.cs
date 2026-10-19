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

        [HttpGet]
        public ActionResult<NoticeResponse> Get()
        {
            return Ok(_noticeService.GetNotice());
        }
    }
}