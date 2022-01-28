
namespace PastryCorner.WebApi.Middleware
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using PastryCorner.Contracts.Definitions;
    using PastryCorner.Infrastructure.Hubs;
    using PastryCorner.WebApi.Models;
    using Serilog;

    public class ClientMessagesMiddleware : IMiddleware
    {
        private readonly ClientInfo _client;
        private readonly ILogger _log;
        private readonly NotifyHub _hubClient;

        public ClientMessagesMiddleware(ClientInfo client, ILogger log, NotifyHub notifyHub)
        {
            _hubClient = notifyHub;
            _client = client;
            _log = log;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (!(_client?.MinClientVersion == null || context.Request?.Headers == null))
            {
                var userAgent = context.Request.Headers["User-Agent"].ToString();
                if (!string.IsNullOrEmpty(userAgent) && userAgent.ToLower().StartsWith(_client.Client.ToLower()))
                {
                    if (Version.TryParse(_client.MinClientVersion, out var minClientVersion))
                    {
                        try
                        {
                            var clientVersion = Version.Parse(userAgent.Split('/')[1].Trim());
                            var messages = ServerMessagesType.None;
                            if (!(clientVersion >= minClientVersion))
                                messages = ServerMessagesType.ClientVersionDeprecated;
                            if (messages != ServerMessagesType.None && _hubClient?.Context?.ConnectionId != null)
                            {
                                var connectionId = _hubClient.Context.ConnectionId;
                                _log.Debug($"Attempting to Send Server Message ({messages}) to ConnectionId: {connectionId}");
                                await _hubClient.SendServerMessagesEventToClientAsync(messages, connectionId).ConfigureAwait(false);
                            }

                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, $"Adding ServerMessages:{ex.Message}, User-Agent:{userAgent}, MinClientVersion:{_client.MinClientVersion}");
                        }
                    }
                }
            }
            await next(context).ConfigureAwait(false);
        }
    }
}
