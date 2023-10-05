using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaService.Models
{
    public class ProducerModel : KafkaManager
    {
        public string Topics { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string Acks { get; set;} = string.Empty;
        public int? MessageSendMaxRetries { get; set; }
        public int? RetryBackoffMs { get; set;}
        public bool? EnableIdempotence { get; set; }

    }
}