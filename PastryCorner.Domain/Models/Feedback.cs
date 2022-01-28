using System;
using System.Collections.Generic;
using System.Text;

namespace PastryCorner.Domain.Models
{
    public class Feedback : BaseBsonModel
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Message { get; set; }

        public DateTime? CreateDateUtc { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
