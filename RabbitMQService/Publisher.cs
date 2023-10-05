using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQService.Model;
using System.Threading.Channels;

namespace RabbitMQService
{
    public class Publisher : Client
    {
        #region Private
        private readonly PublisherModel _rabbitMQPublisherModel;
        #endregion

        #region ctor
        public Publisher(ClientModel rabbitMQClientModel, PublisherModel rabbitMQPublisherModel, ILogger<Publisher> logger) : base(rabbitMQClientModel, logger)
        {
            _rabbitMQPublisherModel = rabbitMQPublisherModel ?? throw new ArgumentNullException(nameof(rabbitMQPublisherModel));

            if (_rabbitMQPublisherModel.ConfirmPublish)
            {
                Channel.ConfirmSelect();
            }
        }
        #endregion

        #region Public Property
        public PublisherModel Model
        {
            get
            {
                return _rabbitMQPublisherModel;
            }
        }
        #endregion

        #region Public Method
        public void Publish(ReadOnlyMemory<byte> payload, bool? mandatory = null, IBasicProperties? basicProperties = null, Action<object?, BasicReturnEventArgs>? basicReturnCallback = null,
            Action<object?, BasicAckEventArgs>? basicAckCallback = null, Action<object?, BasicNackEventArgs>? basicNackCallback = null)
        {
            if(basicProperties == null)
            {
                basicProperties = Channel.CreateBasicProperties();
                basicProperties.DeliveryMode = 2; // persistent
            }
            if (basicReturnCallback != null)
            {
                Channel.BasicReturn += (sender, eventArgs) => basicReturnCallback(sender, eventArgs);
            }
            if (basicAckCallback != null)
            {
                Channel.BasicAcks += (sender, eventArgs) => basicAckCallback(sender, eventArgs);
            }
            if (basicNackCallback != null)
            {
                Channel.BasicNacks += (sender, eventArgs) => basicNackCallback(sender, eventArgs);
            }

            Channel.BasicPublish(_rabbitMQClientModel.Queue.ExchangeName, _rabbitMQClientModel.Queue.RoutingKey,true, basicProperties, payload);
        }

        public IBasicProperties CreateBasicProperties()
        {
            return Channel.CreateBasicProperties();
        }
        #endregion

    }
}