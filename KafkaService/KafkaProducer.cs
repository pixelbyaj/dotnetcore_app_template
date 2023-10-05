using Confluent.Kafka;
using KafkaService.Models;
using Microsoft.Extensions.Logging;
using static Confluent.Kafka.ConfigPropertyNames;

namespace KafkaService
{
    public class KafkaProducer : KafkaClient
    {
        private readonly IProducer<Null, string> _producer;
        private ProducerConfig _producerConfig;

        public KafkaProducer(ProducerModel producerModel, ILogger logger) : base(producerModel, logger)
        {
            if (producerModel == null)
                throw new ArgumentNullException(nameof(producerModel));
            _producerConfig = GetProducerConfig(producerModel);
            _producer = GetProducer(_producerConfig);
        }

        public async Task ProduceAsync(string message)
        {
            if (_producer != null && _producerModel != null)
            {
                try
                {
                    var deliveryReport = await _producer.ProduceAsync(_producerModel.Topics,
                            new Message<Null, string>
                            {
                                Value = message
                            });

                    if (deliveryReport.Status != PersistenceStatus.Persisted)
                    {
                        // delivery might have failed after retries. This message requires manual processing.
                        _logger.LogError($"ERROR: Message not ack'd by all brokers (value: '{message}'). Delivery status: {deliveryReport.Status}");
                    }
                }
                catch (ProduceException<Ignore, string> ex)
                {
                    _logger.LogError($"Permanent error: {ex.Message} for message (value: '{ex.DeliveryResult.Value}')");
                }
            }
        }

        private IProducer<Null, string> GetProducer(ProducerConfig? producerConfig)
        {
            return new ProducerBuilder<Null, string>(producerConfig)
                .SetErrorHandler((_, e) => _logger.LogError($"Code: {e.Code}. Error: {e.Reason}. Is Fatal: {e.IsFatal}"))
                .Build();
        }

        private static ProducerConfig GetProducerConfig(ProducerModel producerModel)
        {
            ProducerConfig producerConfig = new()
            {
                BootstrapServers = producerModel.BootstrapServers,
                ClientId = producerModel.ClientId,
                SecurityProtocol = producerModel.SecurityProtocol?.ToLowerInvariant() == "ssl" ? SecurityProtocol.Ssl : SecurityProtocol.Plaintext,
                SslCaLocation = producerModel.SslCaLocation,
                SslCertificateLocation = producerModel.SslCertificateLocation,
                SslKeyLocation = producerModel.SslKeyLocation,
                MessageSendMaxRetries = producerModel.MessageSendMaxRetries,
                RetryBackoffMs = producerModel.RetryBackoffMs,
                EnableIdempotence = producerModel.EnableIdempotence
            };
            return producerConfig;
        }
    }
}
