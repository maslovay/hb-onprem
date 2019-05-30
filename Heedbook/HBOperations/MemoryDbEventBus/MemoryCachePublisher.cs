using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MemoryDbEventBus.Handlers;
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
                if (!_memoryCache.HasRecords())
                    Thread.Sleep(100);

                var (key, value) = _memoryCache.Dequeue<IMemoryDbEvent>();

                if (value == null)
                    continue;

                var handler = GetHandler(value);
                handler?.Handle(value);

                if (handler.EventStatus == EventStatus.Fail)
                    _memoryCache.Enqueue(key, value);
            }
        }

        private IMemoryDbEventHandler<IMemoryDbEvent> GetHandler(IMemoryDbEvent evt)
        {
            var realType = evt.GetType();

            return _subscriptions[realType.Name] as IMemoryDbEventHandler<IMemoryDbEvent>;
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