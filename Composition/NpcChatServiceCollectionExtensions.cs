using Microsoft.Extensions.Options;
using RPG2D_Server.Features.NpcChat;
using RPG2D_Server.Features.NpcChat.Services;
using RPG2D_Server.Infrastructure.Ai;

namespace RPG2D_Server.Composition
{
    public static class NpcChatServiceCollectionExtensions
    {
        public static IServiceCollection AddNpcChatFeature(this IServiceCollection services)
        {
            services.AddOptions<PythonAiOptions>().BindConfiguration("PythonAi").Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback && uri.AbsolutePath == "/" && uri.Query.Length == 0 && uri.Fragment.Length == 0 && uri.UserInfo.Length == 0, "PythonAi:BaseUrl 必须为无附加路径的本机 HTTP 根地址。").Validate(options => options.RequestTimeoutSeconds is >= 1 and <= 120, "PythonAi:RequestTimeoutSeconds 必须为 1～120。").ValidateOnStart();
            services.AddHttpClient(PythonNpcChatGateway.ClientName, (serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<PythonAiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = Timeout.InfiniteTimeSpan;
            }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false, UseProxy = false });
            services.AddSingleton<INpcChatGateway, PythonNpcChatGateway>();
            services.AddSingleton<INpcChatService, NpcChatService>();
            return services;
        }
    }
}
