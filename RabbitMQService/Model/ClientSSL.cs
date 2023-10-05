using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQService.Model
{
    public class ClientSSL
    {
        public bool Enabled { get; set; }
        public string ServerName { get; set; } = string.Empty;
        public string CertPath { get; set; } = string.Empty;
        public string CertPassphrase { get; set; } = string.Empty;
    }
}
