using Confluent.Kafka;
using KafkaService.Models;
using Microsoft.Extensions.Logging;

namespace KafkaService
{
    public class KafkaConsumer : KafkaClient
    {
        #region private members
        private readonly IConsumer<Ignore, string> _consumer;
        private ConsumerConfig _consumerConfig;
        #endregion

        #region public method
        public KafkaConsumer(ConsumerModel consumerModel, ILogger logger) : base(consumerModel, logger)
        {
            if (consumerModel == null)
                throw new ArgumentNullException(nameof(consumerModel));
            _consumerConfig = GetConsumerConfig(consumerModel);
            _consumer = GetConsumer(_consumerConfig);
        }

        public void Subscribe(Action<string,string> action, CancellationToken cancellationToken)
        {
            if (_consumer != null && _consumerModel != null)
            {
                try
                {
                    _consumer.Subscribe(_consumerModel.Topics);
                    while (true)
                    {
                        try
                        {
                            var consumeResult = _consumer.Consume(cancellationToken);
                            if (consumeResult == null || consumeResult.IsPartitionEOF)
                            {
                                continue;
                            }
                            string? message = consumeResult.Message.Value;
                            if (!string.IsNullOrEmpty(message))
                                action(message,consumeResult.Topic);

                            if (_consumerModel.ManualCommit)
                            {
                                _consumer.Commit();
                            }
                            else if (_consumerConfig.EnableAutoOffsetStore == false)
                            {
                                _consumer.StoreOffset(consumeResult);
                            }
                        }
                        catch (ConsumeException ex)
                        {
                            string message = $"Cosumer Error: Code: {ex.Error.Code}; {ex.Error.Reason}";
                            _logger.LogError(message);
                        }

                    }
                }
                catch (KafkaException ex)
                {
                    string message = $"Cosumer Error: Code: {ex.Error.Code}; {ex.Error.Reason}";
                    _logger.LogError(message);
                }
                finally
                {
                    _consumer.Close();
                }
            }
        }
        #endregion

        #region private methods
        private static ConsumerConfig GetConsumerConfig(ConsumerModel consumerModel)
        {
            var result = Enum.TryParse(consumerModel.AutoOffsetReset, out AutoOffsetReset autoOffsetReset);
            if (!result)
            {
                autoOffsetReset = AutoOffsetReset.Earliest;
            }
            ConsumerConfig consumerConfig = new()
            {
                GroupId = consumerModel.GroupId,
                BootstrapServers = consumerModel.BootstrapServers,
                SecurityProtocol = consumerModel.SecurityProtocol?.ToLowerInvariant() == "ssl" ? SecurityProtocol.Ssl : SecurityProtocol.Plaintext,
                SslCaLocation = consumerModel.SslCaLocation,
                SslCertificateLocation = consumerModel.SslCertificateLocation,
                SslKeyLocation = consumerModel.SslKeyLocation,
                AutoOffsetReset = autoOffsetReset,
            };
            if (consumerModel.EnableAutoCommit != null)
            {
                consumerConfig.EnableAutoCommit = consumerModel.EnableAutoCommit;
                if (consumerModel.AutoCommitIntervalMs != null)
                    consumerConfig.AutoCommitIntervalMs = consumerModel.AutoCommitIntervalMs;
            }

            if (consumerModel.EnableAutoOffsetStore != null)
            {
                consumerConfig.EnableAutoOffsetStore = consumerModel.EnableAutoOffsetStore;
            }

            return consumerConfig;
        }

        private static IConsumer<Ignore, string> GetConsumer(ConsumerConfig consumerConfig)
        {
            return new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        }
        #endregion
    }
}
