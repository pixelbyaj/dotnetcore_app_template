using RabbitMQService.Abstraction;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Confluent.Kafka.ConfigPropertyNames;

namespace StartupServices
{
    internal class EventHandler : IEventHandler
    {
        private readonly string _fileExtension;
        private readonly string _fileOutDirectory;
        public EventHandler(string fileExtension, string fileOutDirectory)
        {
            _fileExtension = fileExtension;
            _fileOutDirectory = fileOutDirectory;
        }

        public async Task Handle(dynamic eventData)
        {
            byte[] body = eventData.Body.ToArray();
            string message = Encoding.UTF8.GetString(body);
            string fileName = $"{_fileOutDirectory}/{DateTime.Now:ddMMyyyyHHmmssfff}${_fileExtension}";
            await File.WriteAllTextAsync(fileName, message);
        }
    }
}
