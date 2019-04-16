using System;
using System.Collections.Generic;
using System.Linq;
using RabbitMqEventBus.Base;

namespace RabbitMqEventBus
{
    //In this case we have 2 types of subscriptions, dynamic type events and static type events
    public class InMemoryEventBusSubscriptionManager : IEventBusSubscriptionsManager
    {
        private readonly List<Type> _eventTypes;
        private readonly Dictionary<String, List<SubscriptionInfo>> _handlers;

        public InMemoryEventBusSubscriptionManager()
        {
            _handlers = new Dictionary<String, List<SubscriptionInfo>>();
            _eventTypes = new List<Type>();
        }

        public event EventHandler<String> OnEventRemoved;


        public Boolean IsEmpty => !_handlers.Keys.Any();

        public void Clear()
        {
            _handlers.Clear();
        }


        public void AddDynamicSubscription<TH>(String eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            DoAddSubscription(typeof(TH), eventName, true);
        }

        public void AddSubscription<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            DoAddSubscription(typeof(TH), eventName, false);
            _eventTypes.Add(typeof(T));
        }

        public void RemoveDynamicSubscription<TH>(String eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            var handlerToRemove = FindDynamicSubscriptionToRemove<TH>(eventName);
            DoRemoveHandler(eventName, handlerToRemove);
        }

        public void RemoveSubscription<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationEvent
        {
            var handlerToRemove = FindSubscriptionToRemove<T, TH>();
            var eventName = GetEventKey<T>();
            DoRemoveHandler(eventName, handlerToRemove);
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();
            return GetHandlersForEvent(key);
        }

        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(String eventName)
        {
            return _handlers[eventName];
        }

        public Boolean HasSubscriptionsForEvent<T>() where T : IntegrationEvent
        {
            var key = GetEventKey<T>();
            return HasSubscriptionsForEvent(key);
        }

        public Boolean HasSubscriptionsForEvent(String eventName)
        {
            return _handlers.ContainsKey(eventName);
        }

        public Type GetEventTypeByName(String eventName)
        {
            return _eventTypes.SingleOrDefault(t => t.Name == eventName);
        }

        public String GetEventKey<T>()
        {
            return typeof(T).Name;
        }


        private void DoAddSubscription(Type handlerType, String eventName, Boolean isDynamic)
        {
            if (!HasSubscriptionsForEvent(eventName)) _handlers.Add(eventName, new List<SubscriptionInfo>());

            if (_handlers[eventName].Any(s => s.HandlerType == handlerType))
                throw new ArgumentException(
                    $"Handler Type {handlerType.Name} already registered for '{eventName}'", nameof(handlerType));

            if (isDynamic)
                _handlers[eventName].Add(SubscriptionInfo.Dynamic(handlerType));
            else
                _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
        }

        private void DoRemoveHandler(String eventName, SubscriptionInfo subsToRemove)
        {
            if (subsToRemove != null)
            {
                _handlers[eventName].Remove(subsToRemove);
                if (!_handlers[eventName].Any())
                {
                    _handlers.Remove(eventName);
                    var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
                    if (eventType != null) _eventTypes.Remove(eventType);
                    RaiseOnEventRemoved(eventName);
                }
            }
        }

        private void RaiseOnEventRemoved(String eventName)
        {
            var handler = OnEventRemoved;
            if (handler != null) OnEventRemoved(this, eventName);
        }

        private SubscriptionInfo FindDynamicSubscriptionToRemove<TH>(String eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            return DoFindSubscriptionToRemove(eventName, typeof(TH));
        }

        private SubscriptionInfo FindSubscriptionToRemove<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = GetEventKey<T>();
            return DoFindSubscriptionToRemove(eventName, typeof(TH));
        }

        private SubscriptionInfo DoFindSubscriptionToRemove(String eventName, Type handlerType)
        {
            if (!HasSubscriptionsForEvent(eventName)) return null;

            return _handlers[eventName].SingleOrDefault(s => s.HandlerType == handlerType);
        }
    }
}