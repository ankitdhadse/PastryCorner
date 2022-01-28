using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PastryCorner.Contracts.Definitions;
using PastryCorner.Contracts.Models;

namespace PastryCorner.Contracts.Endpoint.Events
{
    public interface IHubClient
    {
        Task PastryViewerAdded(int userId, PastryViewerInfo pastryViewerInfo);
        Task PastryViewerRemoved(int userId, int pastryId);
        Task ServerMessagesEvent(ServerMessagesType serverMessagesType);
    }
}
