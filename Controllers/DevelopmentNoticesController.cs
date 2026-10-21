using Microsoft.AspNetCore.Mvc;
using RPG2D_Server.Contracts;
using RPG2D_Server.Filters;
using RPG2D_Server.Services;

namespace RPG2D_Server.Controllers
{
    [ApiController]
    [Route("api/dev/notices")]
    [ServiceFilter(typeof(DevelopmentNoticeAccessFilter))]
    public sealed class DevelopmentNoticesController : ControllerBase
    {
        private readonly INoticeService _noticeService;

        public DevelopmentNoticesController(INoticeService noticeService)
        {
            _noticeService = noticeService;
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<DevelopmentNoticeResponse>> GetById(long id, CancellationToken cancellationToken)
        {
            if (id <= 0)
            {
                ModelState.AddModelError(nameof(id), "公告编号必须为正数。");
                return ValidationProblem(ModelState);
            }

            var notice = await _noticeService.GetByIdAsync(id, cancellationToken);

            if (notice is null)
            {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "公告不存在", detail: "找不到指定公告。");
            }

            return Ok(notice);
        }

        //`[FromBody]`：告诉模型绑定器，**从 Request Body（JSON）读取参数**
        //`CreatedAtAction` 是 REST 创建资源标准返回，对应 HTTP **201 Created**
        //201 含义：资源创建成功
        [HttpPost]
        public async Task<ActionResult<DevelopmentNoticeResponse>> Create([FromBody] NoticeWriteRequest request, CancellationToken cancellationToken)
        {
            var notice = await _noticeService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = notice.Id }, notice);
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] NoticeWriteRequest request, CancellationToken cancellationToken)
        {
            if (!ValidateId(id))
            {
                return ValidationProblem(ModelState);
            }

            if (!await _noticeService.UpdateAsync(id, request, cancellationToken))
            {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "公告不存在", detail: "找不到指定公告。");
            }

            return NoContent();
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
        {
            if (!ValidateId(id))
            {
                return ValidationProblem(ModelState);
            }

            if (!await _noticeService.DeleteAsync(id, cancellationToken))
            {
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "公告不存在", detail: "找不到指定公告。");
            }

            return NoContent();
        }

        private bool ValidateId(long id)
        {
            if (id > 0)
            {
                return true;
            }

            ModelState.AddModelError(nameof(id), "公告编号必须为正数。");
            return false;
        }
    }
}
