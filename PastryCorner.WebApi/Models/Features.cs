using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PastryCorner.WebApi.Models
{
    public class Features
    {
        public bool EnableElasticLog { get; set; }
        public bool EnablePurchaseReport { get; set; }
        public bool EnableExceptionEmail { get; set; }
        public bool EnableClientDeprecationNotification { get; set; }
    }
}
