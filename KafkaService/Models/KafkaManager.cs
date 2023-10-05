using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaService.Models
{
    public class KafkaManager
    {
        public string BootstrapServers { get; set; } = string.Empty;
        public string SecurityProtocol { get; set; } = string.Empty;
        public string SslCaLocation { get; set; } = string.Empty;
        public string SslCertificateLocation { get; set; } = string.Empty;
        public string SslKeyLocation { get; set; } = string.Empty;
    }
}
