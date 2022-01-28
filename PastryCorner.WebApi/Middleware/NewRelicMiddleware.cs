using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace PastryCorner.WebApi.Middleware
{
    public class NewRelicMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context != null && context.Request.Path.HasValue && context.Request.Path.Value.Contains("signalr"))
                NewRelic.Api.Agent.NewRelic.IgnoreTransaction();

            await next(context).ConfigureAwait(false);
        }
    }
}
