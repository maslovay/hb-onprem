using System;
using System.Collections.Generic;

namespace RabbitMqEventBus.Base
{
    public interface IEventBusSubscriptionsManager
    {
        Boolean IsEmpty { get; }
        event EventHandler<String> OnEventRemoved;

        void AddDynamicSubscription<TH>(String eventName)
            where TH : IDynamicIntegrationEventHandler;

        void AddSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        void RemoveSubscription<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationEvent;

        void RemoveDynamicSubscription<TH>(String eventName)
            where TH : IDynamicIntegrationEventHandler;

        Boolean HasSubscriptionsForEvent<T>() where T : IntegrationEvent;
        Boolean HasSubscriptionsForEvent(String eventName);
        Type GetEventTypeByName(String eventName);
        void Clear();
        IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent;
        IEnumerable<SubscriptionInfo> GetHandlersForEvent(String eventName);
        String GetEventKey<T>();
    }
}