using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQService.Model
{
    public class PublisherModel
    {
        public bool ConfirmPublish { get; set; }
        public string FileDirectory { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public string CompletionDirectory { get; set; } = string.Empty;

    }
}
