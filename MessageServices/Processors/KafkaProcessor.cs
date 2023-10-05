using KafkaService;
using KafkaService.Models;
using MessageServices.Helper;
using MessageServices.Interface;
using System.Net;
using System.Timers;

namespace MessageServices.Processors
{
    public class KafkaProcessor : BaseProcessor, IKafkaProcessor
    {
        #region constant
        private const string MAIN_SECTION = "Configuration:Services:Kafka";
        private const string MANAGER_SECTION = "KafkaManagers";
        private const string CONSUMER_SECTION = "Consumers";
        private const string PUBLISHER_SECTION = "Publishers";
        #endregion

        #region private members
        private System.Timers.Timer _timer = new();
        private readonly List<KafkaClient> _consumers;
        private readonly List<ConsumerModel> _consumerConfigs;
        private readonly List<ProducerModel> _producerConfigs;
        private readonly Dictionary<string, KafkaManager> _kafkaManager;
        private readonly int s_processingTimeInMilliseconds;
        private CancellationToken _cancellationToken;
        #endregion

        #region ctor
        public KafkaProcessor(IConfiguration configuration, ILogger<KafkaProcessor> logger) : base(configuration,logger)
        {
            bool.TryParse(_configuration.GetSection(MAIN_SECTION).GetSection("Enabled").Value, out _enabled);            
            s_processingTimeInMilliseconds = Convert.ToInt32(configuration.GetSection($"{MAIN_SECTION}:Producers:ProcessingTimeInMilliseconds").Value);

            _consumers = new();
            _consumerConfigs = new();
            _producerConfigs = new();
            _kafkaManager = new();
        }
        #endregion

        #region public method
        /// <summary>
        /// 
        /// </summary>
        public void Bootstrap()
        {
            if (!_enabled)
            {
                _logger.LogInformation("Kafka service is disabled");
                return;
            }
            var config = _configuration.GetSection(MAIN_SECTION);
            var managerSections = config.GetSection(MANAGER_SECTION);
            var consumerSections = config.GetSection(CONSUMER_SECTION);
            var producerSections = config.GetSection(PUBLISHER_SECTION);

            ConfigureKafkaManager(managerSections);
            ConfigureConsumer(consumerSections);
            ConfigureProducer(producerSections);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void StartProcess(CancellationToken cancellationToken)
        {
            if (!_enabled)
            {
                _logger.LogInformation("Kafka service is disabled");
                return;
            }
            
            _cancellationToken = cancellationToken;
            StartProcess();
        }

        /// <summary>
        /// This method required to call StartProcess method with CancellationToken
        /// </summary>
        public void StartProcess()
        {
            if(_cancellationToken == CancellationToken.None)
            {
                throw new ArgumentNullException("CancelationToken");
            }

            if (_consumerConfigs.Any())
            {
                Task.Run(() =>
                {
                    StartConsumer();
                });
            }
            if (_producerConfigs.Any())
            {
                Task.Run(() =>
                {
                    StartProducer();
                });
            }

        }
        #endregion

        #region private methods
        private void ConfigureKafkaManager(IConfigurationSection managerSections)
        {
            foreach (var managerSection in managerSections.GetChildren())
            {
                var section = ConfigHelper.GetDictionary(managerSection);
                if (section != null)
                {
                    _kafkaManager.Add(managerSection.Key, new KafkaManager
                    {
                        BootstrapServers = section["BootstrapServers"],
                        SecurityProtocol = section["SecurityProtocol"],
                        SslCaLocation = section.ContainsKey("SslCaLocation") ? section["SslCaLocation"] : string.Empty,
                        SslCertificateLocation = section.ContainsKey("SslCertificateLocation") ? section["SslCertificateLocation"] : string.Empty,
                        SslKeyLocation = section.ContainsKey("SslKeyLocation") ? section["SslKeyLocation"] : string.Empty,
                    });
                }
            }
        }

        private void ConfigureConsumer(IConfigurationSection consumerSections)
        {
            var consumerConfig = consumerSections.GetChildren();
            foreach (var config in consumerConfig)
            {
                var detail = ConfigHelper.GetDictionary(config);
                if (detail != null)
                {
                    var manager = _kafkaManager[detail["KafkaManager"]];

                    if (detail["Enabled"] == null && detail["Enabled"].ToLower() == "false")
                        continue;

                    _consumerConfigs.Add(new ConsumerModel
                    {
                        GroupId = detail["GroupId"],
                        Topics = detail["Topics"],
                        BootstrapServers = manager.BootstrapServers,
                        SecurityProtocol = manager.SecurityProtocol,
                        SslCaLocation = manager.SslCaLocation,
                        SslCertificateLocation = manager.SslCertificateLocation,
                        SslKeyLocation = manager.SslKeyLocation,
                        EnableAutoCommit = Convert.ToBoolean(detail["EnableAutoCommit"]),
                        AutoCommitIntervalMs = Convert.ToInt32(detail["AutoCommitIntervalMs"]),
                        EnableAutoOffsetStore = Convert.ToBoolean(detail["EnableAutoOffsetStore"]),
                        AutoOffsetReset = detail["AutoOffsetReset"],
                        ManualCommit = Convert.ToBoolean(detail["ManualCommit"])
                    });
                }
            }
        }

        private void StartConsumer()
        {
            foreach (var consumer in _consumerConfigs)
            {
                KafkaConsumer kafkaConsumer = new(consumer, _logger);
                kafkaConsumer.Subscribe((string message, string topic) =>
                {
                    //Todo: Write your own Subscribe 

                }, _cancellationToken);
                _consumers.Add(kafkaConsumer);
            }
        }

        private void ConfigureProducer(IConfigurationSection producerSections)
        {
            var producerConfig = producerSections.GetChildren();
            foreach (var config in producerConfig)
            {
                if (config.Key == "ProcessingTimeInMilliseconds") continue;

                var detail = ConfigHelper.GetDictionary(config);
                if (detail != null)
                {
                    var manager = _kafkaManager[detail["KafkaManager"]];
                    if (detail["Enabled"] == null && detail["Enabled"].ToLower() == "false")
                        continue;
                    _producerConfigs.Add(new ProducerModel
                    {
                        Topics = detail["Topics"],
                        BootstrapServers = manager.BootstrapServers,
                        SecurityProtocol = manager.SecurityProtocol,
                        SslCaLocation = manager.SslCaLocation,
                        SslCertificateLocation = manager.SslCertificateLocation,
                        SslKeyLocation = manager.SslKeyLocation,
                        Acks = detail["Acks"],
                        ClientId = Dns.GetHostName(),
                        MessageSendMaxRetries = Convert.ToInt32(detail["MessageSendMaxRetries"]),
                        RetryBackoffMs = Convert.ToInt32(detail["RetryBackoffMs"]),
                        EnableIdempotence = Convert.ToBoolean(detail["EnableIdempotence"])
                    });
                }
            }
        }

        private void StartProducer()
        {
            _timer = new System.Timers.Timer(s_processingTimeInMilliseconds);
            _timer.Elapsed += OnTimedOutgoingQueueEventAsync;
            _timer.Enabled = true;
            _timer.Start();

        }

        private void OnTimedOutgoingQueueEventAsync(Object? obj, ElapsedEventArgs eventArgs)
        {
            Parallel.ForEach(_producerConfigs, (producer) =>
            {
                //ToDo: Implement 
            });
        }

        #endregion

    }
}
