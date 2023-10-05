using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaService.Models
{
    public class ConsumerModel : KafkaManager
    {
        public string GroupId { get; set; } = string.Empty;
        public string Topics { get; set; } = string.Empty;
        public bool? EnableAutoCommit { get; set; }
        public int? AutoCommitIntervalMs { get; set; }
        public bool? EnableAutoOffsetStore { get; set; } = false;
        public string AutoOffsetReset { get; set; } = "Earliest";
        public bool ManualCommit { get; set; } = false;
        
    }
}