using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQService.Model
{
    public class ConsumerModel
    {
        public string ConsumerTag { get; set; } = string.Empty;
        public bool AutoAcknowledgment { get; set; }
        public string? FileDirectory { get; set; }
        public string? FileExtension { get; set; }
    }
}
