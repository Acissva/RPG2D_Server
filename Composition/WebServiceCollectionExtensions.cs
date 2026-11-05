using RPG2D_Server.Infrastructure.Web;

namespace RPG2D_Server.Composition
{
    public static class WebServiceCollectionExtensions
    {
        public static IServiceCollection AddWebApi(this IServiceCollection services)
        {
            services.AddControllers();
            services.AddProblemDetails();
            services.AddExceptionHandler<ApiExceptionHandler>();
            services.AddOpenApi();
            return services;
        }
    }
}