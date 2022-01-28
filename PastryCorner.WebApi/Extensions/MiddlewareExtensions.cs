namespace PastryCorner.WebApi.Extensions
{
    using Microsoft.AspNetCore.Builder;
    using Middleware;

    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthorizationMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthorizationMiddleware>();
        }

        public static IApplicationBuilder UseClientMessagesMiddleware(
            this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ClientMessagesMiddleware>();
        }

        public static IApplicationBuilder UseNewRelicMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<NewRelicMiddleware>();
        }
    }
}
