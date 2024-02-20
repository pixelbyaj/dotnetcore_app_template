using RabbitMQService.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RabbitMQService.SubscriptionsManager;

namespace RabbitMQService.Abstraction
{
    public interface ISubscriptionsManager
    {
        bool IsEmpty { get; }
        event EventHandler<string> OnEventRemoved;

        void AddSubscription(string eventName);

        void RemoveSubscription(string eventName);

        bool HasSubscriptionsForEvent(string eventName);

        public void Clear();

    }
}
