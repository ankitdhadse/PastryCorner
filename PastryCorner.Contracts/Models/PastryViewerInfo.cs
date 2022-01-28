using System;
using System.Collections.Generic;
using System.Text;

namespace PastryCorner.Contracts.Models
{
    public class PastryViewerInfo
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int PastryId { get; set; }
        public DateTime StartedViewingTimeUtc { get; set; }
    }
}
