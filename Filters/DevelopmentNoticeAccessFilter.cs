using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace RPG2D_Server.Filters
{
    public class DevelopmentNoticeAccessFilter : IAsyncResourceFilter
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public DevelopmentNoticeAccessFilter(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var remoteAddress = context.HttpContext.Connection.RemoteIpAddress;

            if (remoteAddress is not null && remoteAddress.IsIPv4MappedToIPv6)
            {
                remoteAddress = remoteAddress.MapToIPv4();
            }

            if (!_environment.IsDevelopment()
                || !_configuration.GetValue<bool>("DevelopmentNoticeApi:Enabled")
                || remoteAddress is null
                || !IPAddress.IsLoopback(remoteAddress))
            {
                context.Result = new NotFoundResult();
                return;
            }

            await next();
        }
    }
}
