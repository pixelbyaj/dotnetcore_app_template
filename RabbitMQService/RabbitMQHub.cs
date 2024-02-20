using Microsoft.Extensions.Logging;
using Polly.Retry;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client;
using RabbitMQService.Abstraction;
using RabbitMQService.Events;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Polly;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Channels;

namespace RabbitMQService
{
    public class RabbitMQHub : IRabbitMQHub, IDisposable
    {

        private static readonly JsonSerializerOptions s_indentedOptions = new() { WriteIndented = true };
        private static readonly JsonSerializerOptions s_caseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly string _brokerName;
        private readonly IRabbitMQConnection _persistentConnection;
        private readonly ILogger<RabbitMQHub> _logger;
        private readonly ISubscriptionsManager _subsManager;
        private readonly int _retryCount;

        private IModel _consumerChannel;
        private string _queueName;

        public RabbitMQHub(IRabbitMQConnection persistentConnection, ILogger<RabbitMQHub> logger, ISubscriptionsManager subsManager, string brokerName, string queueName = null, int retryCount = 5)
        {
            _persistentConnection = persistentConnection ?? throw new ArgumentNullException(nameof(persistentConnection));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _brokerName = brokerName ?? throw new ArgumentNullException(nameof(brokerName));
            _subsManager = subsManager ?? new SubscriptionsManager();
            _queueName = queueName;
            _consumerChannel = CreateConsumerChannel();
            _retryCount = retryCount;
            _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
        }

        public void Publish(IntegrationEvent @event, byte[] payload,
            Action<object?, BasicReturnEventArgs>? basicReturnCallback = null,
            Action<object?, BasicAckEventArgs>? basicAckCallback = null,
            Action<object?, BasicNackEventArgs>? basicNackCallback = null)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            var policy = RetryPolicy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s", @event.Id, $"{time.TotalSeconds:n1}");
                });

            var eventName = @event.GetType().Name;
            _logger.LogTrace("Creating RabbitMQ channel to publish event: {EventId} ({EventName})", @event.Id, eventName);

            using var channel = _persistentConnection.CreateModel();
            _logger.LogTrace("Declaring RabbitMQ exchange to publish event: {EventId}", @event.Id);

            channel.ExchangeDeclare(exchange: _brokerName, type: "direct");


            policy.Execute(() =>
            {
                var properties = channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent

                _logger.LogTrace("Publishing event to RabbitMQ: {EventId}", @event.Id);

                if (basicReturnCallback != null)
                {
                    channel.BasicReturn += (sender, eventArgs) => basicReturnCallback(sender, eventArgs);
                }
                if (basicAckCallback != null)
                {
                    channel.BasicAcks += (sender, eventArgs) => basicAckCallback(sender, eventArgs);
                }
                if (basicNackCallback != null)
                {
                    channel.BasicNacks += (sender, eventArgs) => basicNackCallback(sender, eventArgs);
                }

                channel.BasicPublish(
                    exchange: _brokerName,
                    routingKey: @event.GetType().Name,
                    mandatory: true,
                    basicProperties: properties,
                    body: payload);
            });
        }

        public void Subscribe(string eventName, Func<object?, BasicDeliverEventArgs, Task> receivedCallback)
        {
            _subsManager.AddSubscription(eventName);
            StartBasicConsume(receivedCallback);
        }

        public void Unsubscribe(string eventName)
        {
            _subsManager.RemoveSubscription(eventName);
        }

        public void Dispose()
        {
            if (_consumerChannel != null)
            {
                _consumerChannel.Dispose();
            }

            _subsManager.Clear();
        }

        private void StartBasicConsume(Func<object?, BasicDeliverEventArgs, Task> receivedCallback)
        {
            _logger.LogTrace("Starting RabbitMQ basic consume");

            if (_consumerChannel != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

                consumer.Received += async (sender, eventArgs) => {
                    var eventName = eventArgs.RoutingKey;
                    var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

                    _logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);

                    if (_subsManager.HasSubscriptionsForEvent(eventName))
                    {
                        await receivedCallback(sender, eventArgs);
                    }
                    else
                    {
                        _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
                    }

                    // Even on exception we take the message off the queue.
                    // in a REAL WORLD app this should be handled with a Dead Letter Exchange (DLX). 
                    // For more information see: https://www.rabbitmq.com/dlx.html
                    _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                };

                _consumerChannel.BasicConsume(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);
            }
            else
            {
                _logger.LogError("StartBasicConsume can't call on _consumerChannel == null");
            }
        }

        private void SubsManager_OnEventRemoved(object sender, string eventName)
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            using var channel = _persistentConnection.CreateModel();
            channel.QueueUnbind(queue: _queueName,
                exchange: _brokerName,
                routingKey: eventName);

            if (_subsManager.IsEmpty)
            {
                _queueName = string.Empty;
                _consumerChannel.Close();
            }
        }
       
        private IModel CreateConsumerChannel()
        {
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            _logger.LogTrace("Creating RabbitMQ consumer channel");

            var channel = _persistentConnection.CreateModel();

            channel.ExchangeDeclare(exchange: _brokerName,
                                    type: "direct");

            channel.QueueDeclare(queue: _queueName,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: null);

            channel.CallbackException += (sender, ea) =>
            {
                _logger.LogWarning(ea.Exception, "Recreating RabbitMQ consumer channel");

                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
            };

            return channel;
        }

    }
}