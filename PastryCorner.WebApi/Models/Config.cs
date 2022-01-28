using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PastryCorner.WebApi.Models
{
    public class Config
    {
        public ConnectionStrings ConnectionStrings { get; set; }
        public string ScheduleCronExpression { get; set; }
        public string SignalRUrl { get; set; }
        public string MongoDatabase { get; set; }
        public string AllowedHosts { get; set; }
        public string ApiBaseRoute { get; set; }
        public string MinimumLogLevel { get; set; }
        public Features Features { get; set; }
        public ClientInfo Client { get; set; }
    }

    public class ConnectionStrings
    {
        public string MongoConnectionString { get; set; }
        public string RedisConnectionString { get; set; }
        public string NServiceBusTransportConnectionString { get; set; }
    }

    public class ClientInfo
    {
        public string MinClientVersion { get; set; }
        public string Client { get; set; }
    }
}
