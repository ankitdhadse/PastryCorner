
namespace PastryCorner.WebApi.Middleware
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Serilog.Context;

    public class AuthorizationMiddleware
    {
        private static string _correlationIdFromClient;
        private readonly RequestDelegate _next;
        public AuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context)
        {
            _correlationIdFromClient = context.Request.Headers["x-Logging-CorrelationID"];
            if (!string.IsNullOrWhiteSpace(_correlationIdFromClient))
                using (LogContext.PushProperty("CorrelationID", _correlationIdFromClient, true)) { }
            await _next.Invoke(context).ConfigureAwait(false);
        }
    }
}
