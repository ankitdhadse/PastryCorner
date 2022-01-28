using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using PastryCorner.Contracts.Definitions;
using PastryCorner.Contracts.Endpoint.Events;
using PastryCorner.Contracts.Models;
using Serilog;
using StackExchange.Redis;

namespace PastryCorner.Infrastructure.Hubs
{
    public class NotifyHub : Hub<IHubClient>
    {
        private readonly IHubContext<NotifyHub, IHubClient> _hubContext;
        private readonly ILogger _log;
        private const string RedisExceptionName = nameof(RedisConnectionException);

        public NotifyHub(IHubContext<NotifyHub, IHubClient> hubContext)
        {
            _log = Log.Logger;
            _hubContext = hubContext;
        }

        public async Task SendPastryViewerAddedRealtime(int userid, PastryViewerInfo pastryViewer)
        {
            var methodName = nameof(SendPastryViewerAddedRealtime);
            try
            {
                _log.Debug(methodName);
                await _hubContext.Clients.All.PastryViewerAdded(userid, pastryViewer).ConfigureAwait(false);
            }
            catch (RedisConnectionException e)
            {
                _log.Error(e, $"{RedisExceptionName} {methodName} Realtime update failed for UserId: {userid}. {e?.Message}");
            }
            catch (Exception e)
            {
                _log.Error(e, $"Exception: {methodName} Realtime update failed for UserId: {userid}. {e?.Message}");
            }
        }

        public async Task SendPastryViewerRemoved(int userId, int pastryId)
        {
            var methodName = nameof(SendPastryViewerRemoved);
            try
            {
                _log.Debug(methodName);
                await _hubContext.Clients.All.PastryViewerRemoved(userId, pastryId).ConfigureAwait(false);
            }
            catch (RedisConnectionException e)
            {
                _log.Error(e, $"{RedisExceptionName} {methodName} Realtime update failed for UserId: {userId}, PastryId: {pastryId}. {e?.Message}");
            }
            catch (Exception e)
            {
                _log.Error(e, $"Exception: {methodName} Realtime update failed for UserId: {userId}, PastryId: {pastryId}. {e?.Message}");
            }
        }

        public async Task SendServerMessagesEventToClientAsync(ServerMessagesType serverMessagesType, string clientId)
        {
            var methodName = nameof(SendServerMessagesEventToClientAsync);
            try
            {
                _log.Debug(methodName);
                if (string.IsNullOrEmpty(clientId)) return;
                if (_hubContext.Clients != null)
                {
                    await _hubContext.Clients.Client(clientId).ServerMessagesEvent(serverMessagesType).ConfigureAwait(false);
                }
            }
            catch (RedisConnectionException e)
            {
                _log.Error(e, $"{RedisExceptionName} {methodName} Realtime update failed for clientId: {clientId} messageType: {serverMessagesType}. {e?.Message}");
            }
            catch (Exception e)
            {
                _log.Error(e, $"Exception: {methodName} Realtime update failed for clientId: {clientId} messageType: {serverMessagesType}. {e?.Message}");
            }
        }
    }
}
