using System.Text;
using RabbitMQService;
using StartupServices.Interface;
using RabbitMQService.Model;
using StartupServices.Helper;
using System.Timers;
using RabbitMQService.Abstraction;
using RabbitMQService.Events;

namespace StartupServices.Processors
{
    public class RabbitMQProcessor : IRabbitMQProcessor
    {
        #region constants
        private const string QUEUE_MANAGER_SECTION = "QueueManagers";
        private const string CONSUMER_SECTION = "Consumer";
        private const string PUBLISHER_SECTION = "Publisher";
        #endregion

        #region private members
        private readonly IDictionary<string, (string exchangeName, ConsumerModel model, RabbitMQHub rabbitMQHub)> _consumerManagers;
        private readonly IDictionary<string, (PublisherModel model, RabbitMQHub rabbitMQHub)> _publisherManagers;
        private readonly IConfigurationSection _configurationSection;
        private readonly IServiceProvider _serviceProvider;
        private static System.Timers.Timer? s_publisherTimer;
        private static int s_processingTimeInMilliseconds;
        
        #endregion

        #region ctor
        public RabbitMQProcessor(IConfigurationSection configurationSection, ILogger<RabbitMQProcessor> logger, IServiceProvider serviceProvider)
        {
            _publisherManagers = new Dictionary<string, (PublisherModel model, RabbitMQHub rabbitMQHub)>();
            _consumerManagers = new Dictionary<string, (string exchangeName, ConsumerModel model, RabbitMQHub rabbitMQHub)>();
            _configurationSection = configurationSection;
            _serviceProvider = serviceProvider;
            Connect();
        }
        #endregion

        #region private method
        /// <summary>
        /// Configure Consumer and Publisher with it's respective configuration
        /// </summary>
        /// <param name="configuration"></param>

        private void Connect()
        {
            Bootstrap();
        }

        /// <summary>
        /// 
        /// </summary>
        private void Bootstrap()
        {
            var queueManagers = _configurationSection.GetSection(QUEUE_MANAGER_SECTION);
            var consumer = _configurationSection.GetSection(CONSUMER_SECTION);
            var publisher = _configurationSection.GetSection(PUBLISHER_SECTION);

            //Consumer

            var consumerDic = ConfigHelper.GetDictionary(consumer);
            ArgumentNullException.ThrowIfNull(consumerDic, "consumer");
            SetConsumer(consumer.Key, consumerDic, queueManagers);
            StartConsumer();
            //Publisher

            var publisherDic = ConfigHelper.GetDictionary(publisher);
            SetPublisher(publisher.Key, publisherDic, queueManagers);
            StartPublisher();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="consumerKey"></param>
        /// <param name="consumerDic"></param>
        /// <param name="queueManagers"></param>
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
                _consumerManagers.Add(consumerKey, (clientModel.Queue.ExchangeName, rmqcConsumer, GetRabbitMQHub(clientModel)));
            }
        }

        /// <summary>
        /// Activate Consumers
        /// </summary>
        private void StartConsumer()
        {
            Parallel.ForEach(_consumerManagers.Values, consumer =>
            {
                consumer.rabbitMQHub.Subscribe(consumer.exchangeName, async (obj, eventArgs) =>
                {
                    byte[] body = eventArgs.Body.ToArray();
                    string message = Encoding.UTF8.GetString(body);
                    string fileOutDirectory = string.IsNullOrEmpty(consumer.model.FileDirectory) ? Environment.CurrentDirectory : consumer.model.FileDirectory;
                    string fileExtenstion = string.IsNullOrEmpty(consumer.model.FileExtension) ? ".xml" : consumer.model.FileExtension;
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
            Parallel.ForEach(_publisherManagers.Values, publisher =>
            {
                var path = publisher.model.FileDirectory;
                var extenstions = publisher.model.FileExtension;
                var completedProcessPath = publisher.model.CompletionDirectory;
                var files = Directory.EnumerateFiles(path);
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (string.IsNullOrEmpty(extenstions) && !extenstions.Split(";").Contains(fileInfo.Extension))
                    {
                        continue;
                    }
                    publisher.rabbitMQHub.Publish(new IntegrationEvent(), File.ReadAllBytes(file));
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

        private RabbitMQHub? GetPublisher(IDictionary<string, string> publisherDic, IConfigurationSection queueManagers)
        {
            RabbitMQHub? publisher = null;
            
            string queueManagerName = publisherDic["QueueManager"];
            var queueManager = ConfigHelper.GetDictionary(queueManagers.GetSection(queueManagerName));
            if (queueManager != null)
            {
                var clientModel = GetQueueManager(queueManager, publisherDic["QueueName"], publisherDic["ExchangeName"], publisherDic["RoutingKey"]);
                publisher = GetRabbitMQHub(clientModel);
            }
            return publisher;
        }

        private void SetPublisher(string publisherName, IDictionary<string, string> publisherDic, IConfigurationSection queueManagers)
        {
            var publisher = GetPublisher(publisherDic, queueManagers);
            if (publisher != null)
            {
                var rmqcPublisher = new PublisherModel
                {
                    ConfirmPublish = Convert.ToBoolean(publisherDic["ConfirmPublish"]),
                    FileDirectory = publisherDic["FileDirectory"],
                    FileExtension = publisherDic["FileExtension"],
                    CompletionDirectory = publisherDic["CompletionDirectory"],

                };
                _publisherManagers.Add(publisherName, (rmqcPublisher, publisher));
            }

        }

        private RabbitMQHub GetRabbitMQHub(ClientModel clientModel)
        {
            ISubscriptionsManager? eventBusSubscriptionsManager = _serviceProvider.GetRequiredService<ISubscriptionsManager>();
            RabbitMQConnection rabbitMQManager = new (RabbitMQFactory.ConnectionFactory(clientModel), _serviceProvider.GetRequiredService<ILogger<RabbitMQConnection>>(), clientModel.RetryCount);
            return new RabbitMQHub(rabbitMQManager, _serviceProvider.GetRequiredService<ILogger<RabbitMQHub>>(), eventBusSubscriptionsManager, clientModel.Queue.ExchangeName, clientModel.Queue.QueueName, clientModel.RetryCount);

        }
        #endregion

    }
}
