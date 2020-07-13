using System;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus
{
    public interface INotificationPublisher
    {
        void Publish(IntegrationEvent @event);

        void PublishQueue(String queue, String message);

        void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        void SubscribeDynamic<TH>(String eventName)
            where TH : IDynamicIntegrationEventHandler;

        void UnsubscribeDynamic<TH>(String eventName)
            where TH : IDynamicIntegrationEventHandler;

        void Unsubscribe<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationEvent;
    }
}