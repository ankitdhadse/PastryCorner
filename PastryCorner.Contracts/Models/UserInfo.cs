using System;
using System.Collections.Generic;
using System.Text;

namespace PastryCorner.Contracts.Models
{
    public class UserInfo
    {
        public int UserId { get; set; }
        public int Name { get; set; }
        public int Code { get; set; }
        public int CreateDateUtc { get; set; }
        public DateTime CreateByUserId { get; set; }
        public int UpdateDateUtc { get; set; }
        public DateTime UpdateByUserId { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
