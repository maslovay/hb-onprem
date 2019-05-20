using System;
using System.Collections.Generic;
using System.Linq;
using Notifications.Base;
using RabbitMqEventBus;
using RabbitMqEventBus.Base;

namespace UnitTestExtensions
{
    public class RabbitPublisherMock : INotificationPublisher
    {
        private Dictionary<Type, INotificationHandler> subscriptions = new Dictionary<Type, INotificationHandler>(10);
        
        public RabbitPublisherMock()
        {
            
        }

        private INotificationHandler GetHandler(Type _type)
        {
            foreach (var type in subscriptions.Keys)
            {
                if (type == _type)
                    return subscriptions[type];
            }

            return null;
        }
        
        public void Publish(IntegrationEvent @event)
        {
            var concreteType = @event.GetType();
            var handler = GetHandler(concreteType);

            handler?.EventRaised(@event);
        }

        public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            if (!subscriptions.Keys.Contains(typeof(T)))
                subscriptions[typeof(T)] = new NotificationHandlerMock();
        }

        public void SubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
        }

        public void UnsubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
        }

        public void Unsubscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
        }
    }
}