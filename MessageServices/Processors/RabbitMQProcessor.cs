using System.Text;
using RabbitMQService;
using MessageServices.Interface;
using RabbitMQService.Model;
using MessageServices.Helper;
using System.Timers;

namespace MessageServices.Processors
{
    public class RabbitMQProcessor : BaseProcessor, IRabbitMQProcessor
    {
        #region constants
        private const string MAIN_SECTION = "Configuration:Services:RabbitMQ";
        private const string QUEUE_MANAGER_SECTION = "QueueManagers";
        private const string CONSUMER_SECTION = "Consumers";
        private const string PUBLISHER_SECTION = "Publishers";
        #endregion

        #region private members
        private readonly IDictionary<string, Consumer> _consumerManagers;
        private readonly IDictionary<string, Publisher> _publisherManagers;
        private static System.Timers.Timer? s_publisherTimer;
        private static int s_processingTimeInMilliseconds;
        private readonly IServiceProvider _serviceProvider;
        #endregion

        #region ctor
        public RabbitMQProcessor(IConfiguration configuration, ILogger<RabbitMQProcessor> logger,IServiceProvider serviceProvider) : base(configuration, logger)
        {
            bool.TryParse(_configuration.GetSection(MAIN_SECTION).GetSection("Enabled").Value, out _enabled);
            _publisherManagers = new Dictionary<string, Publisher>();
            _consumerManagers = new Dictionary<string, Consumer>();
            _serviceProvider = serviceProvider;
        }
        #endregion

        #region public method
        /// <summary>
        /// Configure Consumer and Publisher with it's respective configuration
        /// </summary>
        /// <param name="configuration"></param>

        public void StartProcess()
        {
            if (!_enabled)
            {
                _logger.LogInformation("RabbitMQ  service is disabled");
                return;
            }

            Task.Run(() => { StartConsumer(); });
            Task.Run(() => { StartPublisher(); });
        }

        /// <summary>
        /// 
        /// </summary>
        public void Bootstrap()
        {
            if (!_enabled)
            {
                _logger.LogInformation("RabbitMQ  service is disabled");
                return;
            }
            var queues = _configuration.GetSection(MAIN_SECTION);
            var queueManagers = queues.GetSection(QUEUE_MANAGER_SECTION);
            var consumers = queues.GetSection(CONSUMER_SECTION);
            var publishers = queues.GetSection(PUBLISHER_SECTION);

            //Consumer
            foreach (var consumer in consumers.GetChildren())
            {
                var consumerDic = ConfigHelper.GetDictionary(consumer);
                if (consumerDic == null) continue;
                SetConsumer(consumer.Key, consumerDic, queueManagers);
            }

            //Publisher
            foreach (var publisher in publishers.GetChildren())
            {
                if (publisher.Key == "ProcessingTimeInMilliseconds")
                {
                    s_processingTimeInMilliseconds = Convert.ToInt32(publisher["ProcessingTimeInMilliseconds"]);
                    continue;
                }
                var publisherDic = ConfigHelper.GetDictionary(publisher);
                if (publisherDic == null) continue;
                SetPublisher(publisher.Key, publisherDic, queueManagers);
            }
        }

        #endregion

        #region private method

        /// <summary>
        /// Activate Consumers
        /// </summary>
        private void StartConsumer()
        {
            if (!_enabled)
            {
                _logger.LogInformation("RabbitMQ  Processing is disabled");
                return;
            }

            Parallel.ForEach(_consumerManagers.Values, (Consumer consumer) =>
            {
                consumer.BindQueue();
                consumer.Consume(async (obj, eventArgs) =>
                {
                    byte[] body = eventArgs.Body.ToArray();
                    string message = Encoding.UTF8.GetString(body);
                    string fileOutDirectory = string.IsNullOrEmpty(consumer.Model.FileDirectory) ? Environment.CurrentDirectory : consumer.Model.FileDirectory;
                    string fileExtenstion = string.IsNullOrEmpty(consumer.Model.FileExtension) ? ".xml" :
                    consumer.Model.FileExtension;
                    string fileName = $"{fileOutDirectory}/{DateTime.Now:ddMMyyyyHHmmssfff}${fileExtenstion}";
                    await File.WriteAllTextAsync(fileName, message);

                });
            });
        }

        /// <summary>
        /// Activate Consumers
        /// </summary>
        private void StartPublisher()
        {

            s_publisherTimer = new(s_processingTimeInMilliseconds);
            s_publisherTimer.Elapsed += (obj, eventArgs) =>
            {
                FileProcessor(obj, eventArgs);
            };
            s_publisherTimer.Enabled = true;
            s_publisherTimer.Start();


        }

        private void FileProcessor(object? obj, ElapsedEventArgs eventArgs)
        {
            Parallel.ForEach(_publisherManagers.Values, (Publisher publisher) =>
                {
                    var path = publisher.Model.FileDirectory;
                    var extenstions = publisher.Model.FileExtension;
                    var completedProcessPath = publisher.Model.CompletionDirectory;
                    var files = Directory.EnumerateFiles(path);
                    foreach (var file in files)
                    {
                        var fileInfo = new FileInfo(file);
                        if (string.IsNullOrEmpty(extenstions) && !extenstions.Split(";").Contains(fileInfo.Extension))
                        {
                            continue;
                        }
                        publisher.BindQueue();
                        publisher.Publish(File.ReadAllBytes(file));
                        if (Directory.Exists(completedProcessPath))
                        {
                            Directory.CreateDirectory(completedProcessPath);
                        }
                        File.Move(file, $"{completedProcessPath}\\{fileInfo.Name}_${DateTime.Now:ddMMyyyyHHmmssfff}", true);
                    }
                });
        }

        private static ClientModel GetQueueManager(IDictionary<string, string> queueManager, string queueName, string exchangeKey, string routingKey)
        {
            int.TryParse(queueManager["RetryCount"], out int retryCount);
            int.TryParse(queueManager["WaitAndRetrySeconds"], out int waitAndRetrySecond);
            bool.TryParse(queueManager["SslEnabled"], out bool sslEnabled);
            return new ClientModel
            {
                HostName = queueManager["HostName"],
                VirtualHostName = queueManager["VirtualHostName"],
                Username = queueManager["Username"],
                Password = queueManager["Password"],
                ClientProvidedName = queueManager["ClientProvidedName"],
                RetryCount = retryCount == 0 ? 3 : retryCount,
                WaitAndRetrySeconds = waitAndRetrySecond == 0 ? 60 : waitAndRetrySecond,
                Queue = new QueueModel()
                {
                    QueueName = queueName,
                    ExchangeName = exchangeKey,
                    RoutingKey = routingKey
                },
                Ssl = new ClientSSL()
                {
                    Enabled = sslEnabled,
                    ServerName = queueManager["ServerName"],
                    CertPath = queueManager["CertPath"],
                    CertPassphrase = queueManager["CertPassphrase"]
                }
            };

        }

        private void SetConsumer(string consumerKey, IDictionary<string, string> consumerDic, IConfigurationSection queueManagers)
        {
            var rmqcConsumer = new ConsumerModel
            {
                ConsumerTag = consumerDic["ConsumerTag"],
                AutoAcknowledgment = Convert.ToBoolean(consumerDic["AutoAcknowledgment"]),
                FileDirectory = consumerDic["FileDirectory"],
                FileExtension = consumerDic["FileExtension"]
            };

            string queueManagerName = consumerDic["QueueManager"];
            var queueManager = ConfigHelper.GetDictionary(queueManagers.GetSection(queueManagerName));
            if (queueManager != null)
            {
                var clientModel = GetQueueManager(queueManager, consumerDic["QueueName"], consumerDic["ExchangeName"], consumerDic["RoutingKey"]);
                _consumerManagers.Add(consumerKey, new Consumer(clientModel, rmqcConsumer, _serviceProvider.GetRequiredService<ILogger<Consumer>>()));
            }
        }

        private Publisher? GetPublisher(IDictionary<string, string> publisherDic, IConfigurationSection queueManagers)
        {
            Publisher? publisher = null;
            var rmqcPublisher = new PublisherModel
            {
                ConfirmPublish = Convert.ToBoolean(publisherDic["ConfirmPublish"]),
                FileDirectory = publisherDic["FileDirectory"],
                FileExtension = publisherDic["FileExtension"],
                CompletionDirectory = publisherDic["CompletionDirectory"],

            };

            string queueManagerName = publisherDic["QueueManager"];
            var queueManager = ConfigHelper.GetDictionary(queueManagers.GetSection(queueManagerName));
            if (queueManager != null)
            {
                var clientModel = GetQueueManager(queueManager, publisherDic["QueueName"], publisherDic["ExchangeName"], publisherDic["RoutingKey"]);
                publisher = new Publisher(clientModel, rmqcPublisher, _serviceProvider.GetRequiredService<ILogger<Publisher>>());
            }

            return publisher;
        }

        private void SetPublisher(string publisherName, IDictionary<string, string> publisherDic, IConfigurationSection queueManagers)
        {
            var publisher = GetPublisher(publisherDic, queueManagers);
            if (publisher != null)
            {
                _publisherManagers.Add(publisherName, publisher);
            }

        }
        #endregion
    }
}
