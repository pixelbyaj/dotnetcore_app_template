using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQService.Model
{
    public class ClientModel
    {
        public string HostName { get; set; } = string.Empty;
        public string VirtualHostName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ClientProvidedName { get; set; } = string.Empty;
        public bool AutomaticRecoveryEnabled { get; set; }
        public bool TopologyRecoveryEnabled { get; set; }
        public QueueModel Queue { get; set; } = new();
        public ClientSSL Ssl { get; set; } = new();

        public int RetryCount { get; set; } = 3;
        public double WaitAndRetrySeconds { get; set; } = 60;
    }    
}
