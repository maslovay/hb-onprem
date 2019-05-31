using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MemoryDbEventBus.Handlers;
using MemoryDbEventBus.Utils;
using Microsoft.Extensions.DependencyInjection;
using RabbitMqEventBus;
using RabbitMqEventBus.Base;

namespace MemoryDbEventBus
{
    public class MemoryCachePublisher : IMemoryDbPublisher
    {
        private readonly IMemoryCache _memoryCache;
        private readonly Dictionary<string, IIntegrationEventHandler> _subscriptions =
            new Dictionary<string, IIntegrationEventHandler>(50);
        private readonly IServiceProvider _servicesProvider;
        
        public MemoryCachePublisher(IMemoryCache memoryCache, IServiceProvider servicesProvider)
        {
            _memoryCache = memoryCache;
            _servicesProvider = servicesProvider;

            Load();
        }

        private void Load()
        {
            var workerThrd = new Thread(ProcessEvents);
            workerThrd.Start();
        }
        
        public void Publish(IMemoryDbEvent @event)
        {
            _memoryCache.Enqueue(@event.Id, @event);
        }

        public void Subscribe<T, TH>() where T : IMemoryDbEvent where TH : IMemoryDbEventHandler<T>
        {
            var typeName = typeof(T).Name;
            if (!CheckExistance(typeof(T)))
                _subscriptions[typeName] = _servicesProvider.GetService<TH>();
        }

        public void Unsubscribe<T, TH>() where T : IMemoryDbEvent where TH : IMemoryDbEventHandler<T>
        {
            var typeName = typeof(T).Name;
            if (CheckExistance(typeof(T)))
                _subscriptions.Remove(typeName);
        }

        private void ProcessEvents()
        {
            while (true)
            {
                if (!_subscriptions.Keys.Any() || !_memoryCache.HasRecords())
                {
                    Thread.Sleep(100);
                    continue;
                }

                var (key, value) = _memoryCache.Dequeue();

                if (value == null)
                    continue;

                var matchingValue = TypeChecker.MatchAndConvert(value, _subscriptions.Keys.ToArray());

                if (matchingValue == null) 
                    continue;

                if (matchingValue.GetType() == typeof(MemoryDbEvent))
                {
                    _memoryCache.Enqueue(((IMemoryDbEvent)matchingValue).Id, value);
                    continue;
                }

                var handler = GetHandler(matchingValue);
                handler?.Handle(matchingValue);

                if (handler == null || handler.EventStatus != EventStatus.Passed) 
                    _memoryCache.Enqueue(key, value);
            }
        }

        private IIntegrationEventHandler GetHandler(IMemoryDbEvent evt)
        {
            var realType = evt.GetType();

            return _subscriptions[realType.Name];
        }

        private bool CheckExistance(Type eventType)
        {
            var eventName = eventType.Name;
            return CheckExistance(eventName);
        }

        private bool CheckExistance(string eventName)
            => _subscriptions.Keys.Any(k => k == eventName);
    }
}