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
        private PipesSender _pipesSender;
        
        public RabbitPublisherMock(PipesSender pipesSender)
        {
            _pipesSender = pipesSender;
        }
        
        public void Publish(IntegrationEvent @event)
        {
            _pipesSender.SendEventMessage(@event);
        }

        public void Subscribe<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T>
        {
            _pipesSender.RegisterPipe<T>();
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