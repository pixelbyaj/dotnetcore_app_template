using RabbitMQService.Abstraction;
using RabbitMQService.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQService
{
    public partial class SubscriptionsManager : ISubscriptionsManager
    {


        private readonly List<string> _handlers;
        private readonly List<Type> _eventTypes;

        public event EventHandler<string> OnEventRemoved;

        public SubscriptionsManager()
        {
            _handlers = new List<string>();
            _eventTypes = new List<Type>();
        }

        public bool IsEmpty => _handlers is { Count: 0 };
        public void Clear() => _handlers.Clear();

        public void AddSubscription(string eventName)
        {
            DoAddSubscription(eventName);
        }

        public bool HasSubscriptionsForEvent(string eventName)
        {
            return _handlers.Contains(eventName);
        }

        private void DoAddSubscription(string eventName)
        {
            if (!_handlers.Contains(eventName))
            {
                _handlers.Add(eventName);
            }
        }

        public void RemoveSubscription(string eventName)
        {
            DoRemoveHandler(eventName);
        }


        private void DoRemoveHandler(string eventName)
        {
            if (_handlers.Contains(eventName))
            {
                _handlers.Remove(eventName);
            }
        }
    }
}
