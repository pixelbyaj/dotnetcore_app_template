using System.Text;
using RabbitMQService;
using StartupServices.Interface;
using RabbitMQService.Model;
using StartupServices.Helper;
using System.Timers;

namespace StartupServices.Processors
{
    public class RabbitMQProcessor : BaseProcessor, IRabbitMQProcessor
    {
        #region constants
        private const string MAIN_SECTION = "Configuration:Services:RabbitMQ";
        private const string QUEUE_MANAGER_SECTION = "QueueManagers";
        private const string CONSUMER_SECTION = "Consumer";
        private const string PUBLISHER_SECTION = "Publisher";
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

        public void Connect()
        {
            Bootstrap();
        }

        /// <summary>
        /// 
        /// </summary>
        private void Bootstrap()
        {
            if (!_enabled)
            {
                _logger.LogInformation("RabbitMQ  service is disabled");
                return;
            }
            var queues = _configuration.GetSection(MAIN_SECTION);
            var queueManagers = queues.GetSection(QUEUE_MANAGER_SECTION);
            var consumer = queues.GetSection(CONSUMER_SECTION);
            var publisher = queues.GetSection(PUBLISHER_SECTION);

            //Consumer
            
                var consumerDic = ConfigHelper.GetDictionary(consumer);
                ArgumentNullException.ThrowIfNull(consumerDic,"consumer");
                SetConsumer(consumer.Key, consumerDic, queueManagers);

            //Publisher
                
                var publisherDic = ConfigHelper.GetDictionary(publisher);
                SetPublisher(publisher.Key, publisherDic, queueManagers);
        }

        public void Subscribe<T>(string eventName)
        {
            throw new NotImplementedException();
        }

        public void Unsubscribe()
        {
            throw new NotImplementedException();
        }

        public void Publish()
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
