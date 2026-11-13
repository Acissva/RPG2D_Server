using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net.Http.Headers;

namespace RPG2D_Server.Infrastructure.Web.Filters
{
    // 这是一个 [ASP.NET](https://ASP.NET) Core 资源过滤器（IAsyncResourceFilter），做成特性 `[Utf8Json]`，可以贴在 Controller 或者 Action 方法上
    // 作用：强制校验当前请求的 Content-Type，必须是 `application/json` 并且编码字符集必须是 utf-8，否则直接返回 415 不支持媒体类型
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class Utf8JsonAttribute : Attribute, IAsyncResourceFilter
    {
        // `context`：当前请求上下文，可以直接短路返回结果（设置 `context.Result` 就不会继续往后走）
        // `next`：委托，调用 `await next()` 代表放行，继续走后续管道（模型绑定、Action 等）
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var contentType = context.HttpContext.Request.ContentType;
            if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaType) || !string.Equals(mediaType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) || (mediaType.CharSet is not null && !string.Equals(mediaType.CharSet.Trim('"'), "utf-8", StringComparison.OrdinalIgnoreCase)))
            {
                var problem = new ProblemDetails { Status = StatusCodes.Status415UnsupportedMediaType, Title = "请求必须使用 UTF-8 JSON。" };
                problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
                var result = new ObjectResult(problem) { StatusCode = problem.Status };
                result.ContentTypes.Add("application/problem+json");
                context.Result = result;
                return;
            }

            await next();
        }
    }
}
