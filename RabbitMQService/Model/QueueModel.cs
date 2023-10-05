using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQService.Model
{
    public class QueueModel
    {
        public string QueueName { get; set; } = string.Empty;
        public string ExchangeName { get; set; } = string.Empty;
        public string RoutingKey { get; set; } = string.Empty;        
    }
}
