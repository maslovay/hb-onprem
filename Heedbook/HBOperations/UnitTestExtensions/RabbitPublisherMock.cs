using RabbitMqEventBus;
using RabbitMqEventBus.Base;

namespace UnitTestExtensions
{
    public class RabbitPublisherMock : INotificationPublisher
    {
        public void Publish(IntegrationEvent @event)
        {
        }

        public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
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