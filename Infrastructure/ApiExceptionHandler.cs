using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RPG2D_Server.Data;
using RPG2D_Server.Validation;

namespace RPG2D_Server.Infrastructure
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
