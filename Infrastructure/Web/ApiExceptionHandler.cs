using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RPG2D_Server.Shared.Data;
using RPG2D_Server.Shared.Network;
using RPG2D_Server.Shared.Validation;

namespace RPG2D_Server.Infrastructure.Web
{
    public class ApiExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<ApiExceptionHandler> _logger;

        public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                return true;
            }

            ProblemDetails problem;

            if (exception is InputValidationException validation)
            {
                problem = new ValidationProblemDetails(validation.Errors) { Status = StatusCodes.Status400BadRequest, Title = "输入数据无效" };
                _logger.LogWarning("请求输入校验失败，TraceId {TraceId}。", httpContext.TraceIdentifier);
            }
            else if (exception is UpstreamServiceException upstream)
            {
                var (status, title) = upstream.Kind switch
                {
                    UpstreamServiceException.FailureKind.Unavailable => (StatusCodes.Status503ServiceUnavailable, "AI 服务暂时不可用"),
                    UpstreamServiceException.FailureKind.Timeout => (StatusCodes.Status504GatewayTimeout, "AI 服务响应超时"),
                    UpstreamServiceException.FailureKind.Busy => (StatusCodes.Status429TooManyRequests, "AI 服务忙碌"),
                    _ => (StatusCodes.Status502BadGateway, "AI 服务返回无效数据")
                };
                problem = new ProblemDetails { Status = status, Title = title, Detail = "请稍后手动重试。" };
                _logger.LogWarning("上游调用失败，Kind {Kind}，TraceId {TraceId}。", upstream.Kind, httpContext.TraceIdentifier);
            }
            else if (exception is BadHttpRequestException badRequest)
            {
                problem = new ProblemDetails { Status = badRequest.StatusCode, Title = badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge ? "请求正文超出大小上限" : "请求格式无效" };
                _logger.LogWarning("HTTP 请求被拒绝，Status {Status}，TraceId {TraceId}。", badRequest.StatusCode, httpContext.TraceIdentifier);
            }
            else
            {
                var unavailable = exception is DataStoreUnavailableException;
                var statusCode = unavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status500InternalServerError;
                problem = new ProblemDetails
                {
                    Status = statusCode,
                    Title = unavailable ? "数据服务暂时不可用" : "服务端处理失败",
                    Detail = unavailable ? "请稍后重试。" : "请联系服务维护者。"
                };
                _logger.LogError("请求失败，异常类型 {ExceptionType}，TraceId {TraceId}。", exception.GetType().Name, httpContext.TraceIdentifier);
            }

            problem.Extensions["traceId"] = httpContext.TraceIdentifier;
            httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
            return true;
        }
    }
}
