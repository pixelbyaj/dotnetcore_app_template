using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQService.Model;
using System.Net.Sockets;
using System.Security.Authentication;

namespace RabbitMQService
{
    /// <summary>
    /// Rabbit MQ Client Abstract Class
    /// </summary>
    public abstract class Client : IDisposable
    {
        private readonly IModel _channel;
        private readonly object _syncRoot = new();
        protected readonly ClientModel _rabbitMQClientModel;
        private readonly int _retryCount;
        private readonly double _waitRetrySeconds;
        private IConnection _connection;
        protected ILogger _logger;
        private bool _disposed;

        protected Client(ClientModel rabbitMQClientModel, ILogger<Client> logger)
        {
            _rabbitMQClientModel = rabbitMQClientModel ?? throw new ArgumentNullException(nameof(rabbitMQClientModel));
            try
            {
                _connection = ConnectionFactory.CreateConnection();
                _channel = _connection.CreateModel();
                _logger = logger;
                _retryCount = rabbitMQClientModel.RetryCount;
                _waitRetrySeconds = rabbitMQClientModel.WaitAndRetrySeconds;
            }
            catch (BrokerUnreachableException e)
            {
                throw e;
            }
        }

        #region Protected Property

        /// <summary>
        /// Rabbit MQ Channel
        /// </summary>
        protected IModel Channel
        {
            get
            {
                lock (_channel)
                {
                    return _channel;
                }
            }
        }
        protected bool IsConnected => _connection is { IsOpen: true } && !_disposed;

        #endregion

        #region Public Method
        /// <summary>
        /// Rabbit MQ Bind 
        /// </summary>
        /// <param name="arguments"></param>
        public void BindQueue(IDictionary<string, object>? arguments = null)
        {
            if (!IsConnected)
            {
                TryConnect();
            }
            _logger.LogTrace("Creating RabbitMQ consumer binding");
            _channel.QueueBind(_rabbitMQClientModel.Queue.QueueName, _rabbitMQClientModel.Queue.ExchangeName, _rabbitMQClientModel.Queue.RoutingKey, arguments);
        }
        #endregion

        #region Private Method
        private bool TryConnect()
        {
            lock (_syncRoot)
            {
                var policy = RetryPolicy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(_waitRetrySeconds), (ex, time) =>
                    {
                        _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s", $"{time.TotalSeconds:n1}");
                    }
                );

                policy.Execute(() =>
                {
                    _connection = ConnectionFactory.CreateConnection();
                });

                if (IsConnected)
                {
                    _connection.ConnectionShutdown += OnConnectionShutdown;
                    _connection.CallbackException += OnCallbackException;
                    _connection.ConnectionBlocked += OnConnectionBlocked;

                    _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);

                    return true;
                }
                else
                {
                    _logger.LogCritical("Fatal error: RabbitMQ connections could not be created and opened");

                    return false;
                }
            }
        }
        private ConnectionFactory ConnectionFactory
        {
            get
            {
                ConnectionFactory connectionFactory = new()
                {
                    UserName = _rabbitMQClientModel.Username,
                    Password = _rabbitMQClientModel.Password,
                    VirtualHost = _rabbitMQClientModel.VirtualHostName,
                    HostName = _rabbitMQClientModel.HostName
                };
                string fClientProvidedName = _rabbitMQClientModel.ClientProvidedName;
                if (!string.IsNullOrEmpty(fClientProvidedName))
                {
                    connectionFactory.ClientProvidedName = fClientProvidedName;
                }
                connectionFactory.AutomaticRecoveryEnabled = _rabbitMQClientModel.AutomaticRecoveryEnabled;
                connectionFactory.TopologyRecoveryEnabled = _rabbitMQClientModel.TopologyRecoveryEnabled;
                if (_rabbitMQClientModel.Ssl.Enabled)
                {
                    connectionFactory.AuthMechanisms = new IAuthMechanismFactory[] { new ExternalMechanismFactory() };
                    connectionFactory.Ssl.Enabled = true;
                    connectionFactory.Ssl.ServerName = _rabbitMQClientModel.Ssl.ServerName;
                    connectionFactory.Ssl.CertPath = _rabbitMQClientModel.Ssl.CertPath;
                    connectionFactory.Ssl.CertPassphrase = _rabbitMQClientModel.Ssl.CertPassphrase;
                    connectionFactory.Ssl.Version = SslProtocols.Tls12;
                }
                return connectionFactory;
            }
        }
        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

            TryConnect();
        }
        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            TryConnect();
        }
        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            TryConnect();
        }
        #endregion

        #region Finalize & Dispose Method
        ~Client()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(disposing: false) is optimal in terms of
            // readability and maintainability.
            Dispose(disposing: false);
        }
        public void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _connection.ConnectionShutdown -= OnConnectionShutdown;
                    _connection.CallbackException -= OnCallbackException;
                    _connection.ConnectionBlocked -= OnConnectionBlocked;
                    _channel.Dispose();
                    _connection.Dispose();
                    _disposed = true;
                }
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);

        }
        #endregion
    }
}