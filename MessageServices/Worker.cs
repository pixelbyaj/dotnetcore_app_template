using StartupServices.Interface;
using StartupServices.Processors;
using System.Data;

namespace StartupServices
{
    public class Worker : BackgroundService
    {
        #region private instance variable
        private readonly ILogger<Worker> _logger;
        private readonly IRabbitMQProcessor _rabbitMQProcessor;
        private readonly IKafkaProcessor _kafkaProcessor;
        #endregion

        #region ctor
        public Worker(ILogger<Worker> logger,
            IRabbitMQProcessor rabbitMQProcessor,
            IKafkaProcessor kafkaProcessor)
        {
            _logger = logger;
            _rabbitMQProcessor = rabbitMQProcessor;
            _kafkaProcessor = kafkaProcessor;
        }
        #endregion

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_rabbitMQProcessor.Enabled)
            {
                ThreadPool.QueueUserWorkItem(BootStrapRabbitMQ);
            }
            else
            {
                _logger.LogInformation("RabbitMQ service is disabled");
            }

            if (_kafkaProcessor.Enabled)
            {
                ThreadPool.QueueUserWorkItem(BootStrapKafkaMonitor, stoppingToken);
            }
            else
            {
                _logger.LogInformation("Kafka service is disabled");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(Int32.MaxValue, stoppingToken);
            }
        }

        #region Private Bootstrap methods
        
        private void BootStrapRabbitMQ(Object? stateInfo)
        {
            _rabbitMQProcessor.Bootstrap();
            _rabbitMQProcessor.StartProcess();
            _logger.LogInformation("RabbitMQ consumers started successfully");
        }

        private void BootStrapKafkaMonitor(Object? state)
        {
            if (state != null)
            {
                CancellationToken stoppingToken = (CancellationToken)state;
                _kafkaProcessor.Bootstrap();
                _kafkaProcessor.StartProcess(stoppingToken);
                _logger.LogInformation("Kafka started successfully");
            }
        }
        #endregion

    }
}