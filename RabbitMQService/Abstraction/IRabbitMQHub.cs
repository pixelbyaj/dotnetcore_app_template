using RabbitMQ.Client.Events;
using RabbitMQService.Events;

namespace RabbitMQService.Abstraction
{
    public interface IRabbitMQHub
    {
        void Publish(IntegrationEvent @event, byte[] payload,
            Action<object?, BasicReturnEventArgs>? basicReturnCallback = null,
            Action<object?, BasicAckEventArgs>? basicAckCallback = null,
            Action<object?, BasicNackEventArgs>? basicNackCallback = null);

        void Subscribe(string eventName, Func<object?, BasicDeliverEventArgs, Task> receivedCallback);
        
        void Unsubscribe(string eventName);
    }
}
